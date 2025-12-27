using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Xml;
using System.Xml.Serialization;
using Opc.UaFx;
using Opc.Ua;


namespace Rfid_Service
{
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    }

    public partial class RfidService : ServiceBase
    {
        public static List<RunOpc> OpcClients = new List<RunOpc>();
        public static readonly List<ServiceConfig> ServiceConfigs = new List<ServiceConfig>();
        private EventLog _eventLog;

        public RfidService()
        {
            InitializeComponent();
        }

        //Import SetServiceStatus function from DLL
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            #region Initialize Even Log

            const string eventSourceName = "RFID Readers";
            const string logName = "RFID Readers Events";

            _eventLog = new EventLog();

            if (!EventLog.SourceExists(eventSourceName))
                EventLog.CreateEventSource(eventSourceName, logName);
            else
                _eventLog.BeginInit();


            _eventLog.Source = eventSourceName;
            _eventLog.Log = logName;

            #endregion

            try
            {
                #region ServiceStatusUpdate

                //Set Status as initializing
                var serviceStatus = new ServiceStatus
                {
                    dwCurrentState = ServiceState.SERVICE_START_PENDING,
                    dwWaitHint = 100000
                };
                SetServiceStatus(ServiceHandle, ref serviceStatus);
                _eventLog.WriteEntry("Initializing", EventLogEntryType.Information);

                #endregion

                #region GatherSettings

                var settingsFile = AppDomain.CurrentDomain.BaseDirectory + "\\Settings\\settings.xml";
                //Directory DOES NOT Exist but file does not
                if (!Directory.Exists(Path.GetDirectoryName(settingsFile)))
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Settings");
                    _eventLog.WriteEntry($"Creating Settings File at {settingsFile}.\nExiting",
                        EventLogEntryType.FailureAudit);
                    CreateFile(settingsFile);
                    Environment.Exit(1);
                }
                //Directory DOES Exist but file does not
                else if (!File.Exists(settingsFile))
                {
                    _eventLog.WriteEntry($"Creating Settings File at {settingsFile}.\nExiting",
                        EventLogEntryType.FailureAudit);
                    CreateFile(settingsFile);
                    Environment.Exit(1);
                }
                //File Exists -- Assuming CAN READ
                else
                {
                    try
                    {
                        var xmlSerializer = new XmlSerializer(typeof(ServiceConfig));
                        using (var reader = new StreamReader(settingsFile))
                        {
                            try
                            {
                                var config = (ServiceConfig)xmlSerializer.Deserialize(reader);
                                config.Readers.RemoveAt(0);
                                config.Server.RemoveAt(0);
                                config.Readers[config.Readers.Count - 1].Uuid.UidGuid = Guid.NewGuid();
                                ServiceConfigs.Add(config);
                            }
                            catch (XmlException xmlException)
                            {
#if DEBUG
                                Debugger.Log(1, "Serialization Error",
                                    "Unable to Deserialize\n\n" + xmlException.Message + "\n\n" +
                                    xmlException.StackTrace);
#endif

                                _eventLog.WriteEntry(
                                    xmlException.Message + "\n\nOccured at Line " + xmlException.LineNumber +
                                    ", Position " + xmlException.LinePosition,
                                    EventLogEntryType.Error, (int)EventIds.Ids.UnknownError);

                                Environment.Exit(1);
                            }

                            Licenser.LicenseKey =
                                "";
                        }
                    }
                    catch (Exception e)
                    {
                        _eventLog.WriteEntry($"Unknown Error occured.\n\n{e.Source}\n\n{e.StackTrace}\n\n{e.Message}",
                            EventLogEntryType.Error);
                        Environment.Exit(1);
                    }
                }

                #endregion

                #region ConfigureOpc

                foreach (var serviceConfig in ServiceConfigs)
                foreach (var server in serviceConfig.Server)
                {
                    var client = new RunOpc(ref _eventLog, server);
                    foreach (var reader in serviceConfig.Readers)
                    {
                        client.SubscribeNode(reader.Tags.ReaderTag.ReadTag);
                        if (reader.Tags.WriterTag.WriteTag != null)
                            client.SubscribeNode(reader.Tags.WriterTag.WriteTag);
                    }

                    OpcClients.Add(client);
                }

                #endregion
            }
            catch (Exception e)
            {
                //General Exception Catch
#if DEBUG
                Debugger.Log(4, e.Source, e.Message + "\n\n" + e.InnerException?.Message + "\n\n" + e.StackTrace);
#endif

                #region SetStatusUpdate

                _eventLog.WriteEntry($"Unknown Error occured.\n\n{e.Source}\n\n{e.StackTrace}\n\n{e.Message}",
                    EventLogEntryType.Error);
                var serviceStatus = new ServiceStatus
                {
                    dwCurrentState = ServiceState.SERVICE_STOP_PENDING,
                    dwWaitHint = 1000
                };
                SetServiceStatus(ServiceHandle, ref serviceStatus);

                _eventLog.WriteEntry("Stopping", EventLogEntryType.Information);

                serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
                SetServiceStatus(ServiceHandle, ref serviceStatus);
                Environment.Exit(1);

                #endregion
            }
        }

        #region CreateFile

        /// <summary>
        ///     Creates file for sample output as no file exsists.
        /// </summary>
        /// <param name="settingsFile">Path to the settings file</param>
        private static void CreateFile(string settingsFile)
        {
            var config = new ServiceConfig();

            var xmlSerializer = new XmlSerializer(typeof(ServiceConfig));
            using (var writer = new StreamWriter(settingsFile))
            {
                xmlSerializer.Serialize(writer, config);
            }
        }

        #endregion

        protected override void OnStop()
        {
        }
    }
}