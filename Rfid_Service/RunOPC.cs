using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;

namespace Rfid_Service
{
    public class RunOpc
    {
        private readonly OpcClient _client = new OpcClient();
        private EventLog _eventLog;
        public RunOpc(ref EventLog log, ServiceConfig.OPC config)
        {

            _eventLog = log;


            
            _client.ApplicationName = AppDomain.CurrentDomain.FriendlyName;
            _client.ApplicationUri = new Uri($"urn::{AppDomain.CurrentDomain.FriendlyName}");
            _client.SessionName = "Server Monitoring";
            _client.ServerAddress = new Uri($"opc.tcp:\\\\{config.ServerAddress}:{config.ServerPort}");
            

            
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Settings\\OpcConfiguration\\config.xml"))
            {
                if (config.Auth.AnonymousLogin != null && config.Auth.AnonymousLogin == false)
                {
                    try
                    {

                        // Default: ".\CertificateStores\Trusted"
                        _client.CertificateStores.ApplicationStore.Path
                            = AppDomain.CurrentDomain.BaseDirectory + "Settings\\App Certificates";
                        if (!Directory.Exists(_client.CertificateStores.ApplicationStore.Path))
                        {
                            Directory.CreateDirectory(_client.CertificateStores.ApplicationStore.Path);
                        }

                        // Default: ".\CertificateStores\Rejected"
                        _client.CertificateStores.RejectedStore.Path
                            = AppDomain.CurrentDomain.BaseDirectory + "Settings\\Rejected Certificates";
                        if (!Directory.Exists(_client.CertificateStores.RejectedStore.Path))
                        {
                            Directory.CreateDirectory(_client.CertificateStores.RejectedStore.Path);
                        }

                        // Default: ".\CertificateStores\Trusted"
                        _client.CertificateStores.TrustedIssuerStore.Path
                            = AppDomain.CurrentDomain.BaseDirectory + "Settings\\Trusted Issuer Certificates";
                        if (!Directory.Exists(_client.CertificateStores.TrustedIssuerStore.Path))
                        {
                            Directory.CreateDirectory(_client.CertificateStores.TrustedIssuerStore.Path);
                        }

                        // Default: ".\CertificateStores\Trusted"
                        _client.CertificateStores.TrustedPeerStore.Path
                            = AppDomain.CurrentDomain.BaseDirectory + "Settings\\Trusted Peer Certificates";
                        if (!Directory.Exists(_client.CertificateStores.TrustedPeerStore.Path))
                        {
                            Directory.CreateDirectory(_client.CertificateStores.TrustedPeerStore.Path);
                        }

   
                        var certificate = OpcCertificateManager.CreateCertificate(_client);
                        _client.CertificateStores.ApplicationStore.Add(certificate);
                        _client.Certificate = certificate;
                        OpcCertificateManager.SaveCertificate("Certificate", certificate);

                        _client.Security.UserIdentity = new OpcClientIdentity(config.Auth.Username, config.Auth.Password);
                        _client.Security.EndpointPolicy = new OpcSecurityPolicy(OpcSecurityMode.SignAndEncrypt, OpcSecurityAlgorithm.Auto);
                        _client.Security.VerifyServersCertificateDomains = true;
                        _client.Security.AutoAcceptUntrustedCertificates = true;

                        _client.CertificateValidationFailed += ClientOnCertificateValidationFailed;
                    }
                    catch (Exception e)
                    {
                        _eventLog.WriteEntry($"Error with OPC Certification.\n{e.Message}\n\n{e.InnerException}\n\n{e.StackTrace}", EventLogEntryType.Error, (int)EventIds.Ids.OpcCertificateError);
                        Environment.Exit(1);
                    }

                }
                else
                {
                    _client.Security.AutoAcceptUntrustedCertificates = true;
                }

                _client.Configuration.ApplicationName = _client.ApplicationName;
                _client.Configuration.ApplicationType = ApplicationType.Client;
                _client.Configuration.ApplicationUri = _client.ApplicationUri.AbsoluteUri;
                _client.Configuration.CertificateValidator = new CertificateValidator();
                _client.Configuration.ClientConfiguration.DefaultSessionTimeout = 10;
                _client.Configuration.ProductUri = _client.ApplicationName;
                _client.Configuration.Validate();
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Settings\\OpcConfiguration"))
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Settings\\OpcConfiguration");
                _client.Configuration.SaveToFile(AppDomain.CurrentDomain.BaseDirectory + "Settings\\OpcConfiguration\\config.xml");
            }
            else
            {
                _client.Configuration = OpcApplicationConfiguration.LoadClientConfigFile(AppDomain.CurrentDomain.BaseDirectory + "Settings\\OpcConfiguration\\config.xml");
                if (config.Auth.AnonymousLogin != null && config.Auth.AnonymousLogin == false)
                {
                    try
                    {
                        // Default: ".\CertificateStores\Trusted"
                        _client.CertificateStores.ApplicationStore.Path
                          = AppDomain.CurrentDomain.BaseDirectory + "Settings\\App Certificates";

                        
                        var certPath = _client.CertificateStores.ApplicationStore.Path + 
                                      $"\\private\\{_client.ApplicationName} [{_client.Configuration.SecurityConfiguration.ApplicationCertificate.Thumbprint}].pfx";


                        _client.Certificate = OpcCertificateManager.LoadCertificate(certPath);
                        _client.CertificateStores.RejectedStore.GetCertificates();
                        _client.CertificateStores.TrustedIssuerStore.GetCertificates();
                        _client.CertificateStores.TrustedPeerStore.GetCertificates();
                        

                        _client.Security.UserIdentity = new OpcClientIdentity(config.Auth.Username, config.Auth.Password);
                        _client.Security.EndpointPolicy = new OpcSecurityPolicy(OpcSecurityMode.SignAndEncrypt, OpcSecurityAlgorithm.Auto);
                        _client.Security.VerifyServersCertificateDomains = true;
                        _client.Security.AutoAcceptUntrustedCertificates = true;

                        _client.CertificateValidationFailed += ClientOnCertificateValidationFailed;
                    }
                    catch (Exception e)
                    {
                        _eventLog.WriteEntry($"Error with OPC Certification.\n{e.Message}\n\n{e.StackTrace}\n\n{e.InnerException}", EventLogEntryType.Error, (int)EventIds.Ids.OpcCertificateError);
                        Environment.Exit(1);
                    }

                }
                else
                {
                    _client.Security.AutoAcceptUntrustedCertificates = true;
                }
            }

            _client.Connect();

        }

        public int SubscribeNode(OpcNodeId nodeId)
        {
            try
            {
                _client.SubscribeDataChange(nodeId, TagValueChanged);
                
                return 0;
            }
            catch (OpcException e)
            {
                _eventLog.WriteEntry($"Connection Failed. \n\tError Code:{e.Code}\n\tMessage:{e.Message}\n\tCause:{e.Result}", EventLogEntryType.Error, (int)EventIds.Ids.OpcConnectionFailure);
                return 1;
            }
        }

        public void TagValueChanged(object sender, OpcDataChangeReceivedEventArgs e)
        {
            var nodeId = e.MonitoredItem.NodeId;
            var value = _client.ReadNode(nodeId).Value;

            if ((bool)value == false)
            {
                return;
            }

            if (nodeId.ToString().ToLower().Contains("write"))
            {
                WriteTagChange(nodeId);
            }
            else if (nodeId.ToString().ToLower().Contains("read"))
            {
                ReadTagChange(nodeId);
            }
            else
            {
                _eventLog.WriteEntry("No option on this node. Please revise",EventLogEntryType.Warning,(int) EventIds.Ids.OpcTagNotRecognized);
            }
        }

        private int WriteTagChange(OpcNodeId nodeId)
        {
            try
            {
                ServiceConfig.Reader readerConfig = null;
                foreach (var serviceConfig in RfidService.ServiceConfigs)
                {
                    readerConfig =
                        serviceConfig.Readers.FirstOrDefault(reader =>
                            reader.Tags.WriterTag.WriteTag == nodeId.ToString());
                    if (readerConfig != null) break;
                }

                if (readerConfig == null)
                {
                    _eventLog.WriteEntry("Unable to find matching config. Check system settings.",EventLogEntryType.Error,(int) EventIds.Ids.NullReaderError);
                    _client.WriteNode(nodeId, false);
                    return 1;
                }
                using (var reader = new RfidReader(readerConfig,ref _eventLog))
                {
                    if (!reader.GetStatus())
                    {
                        _client.WriteNode(readerConfig.Tags.ErrorTag.ErrorBool, true);
                        _client.WriteNode(readerConfig.Tags.ErrorTag.ErrorString, "Reader not online!");
                        _client.WriteNode(nodeId, false);
                        return 1;
                    }

                    if (string.IsNullOrEmpty(reader.Read_Epc()))
                    {
                        _client.WriteNode(readerConfig.Tags.ErrorTag.ErrorBool, true);
                        _client.WriteNode(readerConfig.Tags.ErrorTag.ErrorString, "No Tags Detected!");
                        _client.WriteNode(readerConfig.Tags.UuidTag.UuidTag, string.Empty);
                    }
                    else
                    {
                        Task.Delay(500);
                        _client.WriteNode(readerConfig.Tags.UuidTag.UuidTag, reader.WriteEpc());
                    }
                    _client.WriteNode(nodeId, false);

                }
                return 0;
            }
            catch (Exception e)
            {
                _eventLog.WriteEntry($"Unknown Error\n{e.Message}\n{e.StackTrace}\n\n{e.InnerException}",EventLogEntryType.Error,(int)EventIds.Ids.UnknownError);
                return 1;
            }
        }

        private int ReadTagChange(OpcNodeId nodeId)
        {
            try
            {
                ServiceConfig.Reader readerConfig = null;
                foreach (var serviceConfig in RfidService.ServiceConfigs)
                {
                    readerConfig =
                        serviceConfig.Readers.FirstOrDefault(reader =>
                            reader.Tags.ReaderTag.ReadTag == nodeId.ToString());
                    if (readerConfig != null) break;
                }

                if (readerConfig == null)
                {
                    _eventLog.WriteEntry("Unable to find matching config. Check system settings.", EventLogEntryType.Error, (int)EventIds.Ids.NullReaderError);
                    _client.WriteNode(nodeId, false);
                    return 1;
                }

                using (var reader = new RfidReader(readerConfig, ref _eventLog))
                {
                    if (!reader.GetStatus())
                    {
                        _client.WriteNode(readerConfig.Tags.ErrorTag.ErrorBool, true);
                        _client.WriteNode(readerConfig.Tags.ErrorTag.ErrorString, "Reader not online!");
                        _client.WriteNode(nodeId, false);
                        return 1;
                    }

                    var nodeVal = reader.Read_Epc();
                    if (string.IsNullOrEmpty(nodeVal))
                    {
                        _client.WriteNode(readerConfig.Tags.ErrorTag.ErrorBool, true);
                        _client.WriteNode(readerConfig.Tags.ErrorTag.ErrorString, "No Tags Detected!");
                        _client.WriteNode(readerConfig.Tags.UuidTag.UuidTag, string.Empty);
                    }
                    else
                    {
                        _client.WriteNode(readerConfig.Tags.UuidTag.UuidTag, nodeVal);
                    }
                    _client.WriteNode(nodeId, false);
                }
                return 0;
            }
            catch (Exception e)
            {
                _eventLog.WriteEntry($"Unknown Error\n{e.Message}\n{e.StackTrace}\n\n{e.InnerException}", EventLogEntryType.Error, (int)EventIds.Ids.UnknownError);
                return 1;
            }
        }

        private static void ClientOnCertificateValidationFailed(object sender, OpcCertificateValidationFailedEventArgs e)
        {
            var client = (OpcClient) sender;

            if(!client.CertificateStores.TrustedPeerStore.Contains(e.Certificate))
            {
                client.CertificateStores.TrustedPeerStore.Add(e.Certificate);
            }

            e.Accept = true;
        }
    }
}
