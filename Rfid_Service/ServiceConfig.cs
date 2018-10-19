using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Net;
using System.Xml;
using System.Xml.Serialization;

namespace Rfid_Service
{
    [Serializable]

    [XmlRoot("RFID_Configuration")]
    public class ServiceConfig
    {
        [XmlArrayItem("Reader", typeof(Reader))]
        [XmlArray("Readers")]
        public List<Reader> Readers { get; set; } = new List<Reader>();

        [XmlArrayItem("Server", typeof(OPC))]
        [XmlArray("OPC")]
        public List<OPC> Server { get; set; } = new List<OPC>();

        internal ServiceConfig()
        {

            for (int i = 8; i < 13; i++)
            {
                var reader = new Reader
                {
                    Comment = $"\n        Readers should be of format[Reader #] and TAG should be [Readers.Read_Tag_#]\n        i.e.Reader {i} \u21d4 Readers.Read_Tag_{i}\n      ",
                    Address = IPAddress.Loopback.ToString(),
                    PortNum = 6000,
                    ReaderName = $"Reader {i}",
                    Tags = new Reader.ReaderTags()
                    {
                        ReaderTag = new Reader.ReaderTags.ReadTags()
                        {
                            ReadTag = $":RFID Readers.Readers.ReadTags.Read_Tag_{i}",
                            Note = "Required, Type BOOLEAN"
                        },

                        WriterTag = new Reader.ReaderTags.WriteTags()
                        {
                            Note = "Optional, Type BOOLEAN",
                            WriteTag = $"2:RFID Readers.Readers.WriteTags.Write_Tag_{i}"
                        },

                        UuidTag = new Reader.ReaderTags.UidTags()
                        {
                            Note = "Required, Type STRING, Tag of unique ID of RFID Tag",
                            UuidTag = $"2:RFID Readers.Readers.UuidTags.Uuid_Tag_{i}"
                        },
                        ErrorTag = new Reader.ReaderTags.ErrorTags()
                        {
                            Note = "Required, Used for Alarms and Events. Type BOOLEAN,STRING",
                            ErrorBool = $"2:RFID Readers.Readers.AE.Error_B_Station_{i}",
                            ErrorString = $"2:RFID Readers.Readers.AE.Error_S_Station_{i}"
                        }

                    },
                    TimeOut = 10,
                    Uuid = new Reader.Uid()
                    {
                        UidGuid = Guid.NewGuid(),
                        UidNote = "LEAVE THIS BLANK"
                    },
                    Active = false
                    
                };
                Readers.Add(reader);
            }
            
            var opc = new OPC()
            {
                ServerAddress = IPAddress.Loopback.ToString(),
                ServerPort = 49320,
                Auth = new OPC.LogonAuth()
                {
                    Username = "Administrator",
                    Password = "Nk73eapl",
                    AnonymousLogin = false,
                    Note = "Provide either Username and Password OR AnonymousLogin as true"
                }

            };
            Server.Add(opc);
        }



        public class Reader
        {
            [XmlElement("Comment", IsNullable = true)]
            public string Comment { get; set; }

            [XmlElement("Location")]
            public string ReaderName { get; set; }


            [XmlElement("Uuid",IsNullable = true)]
            public Uid Uuid { get; set; }

            public class Uid
            {
                [XmlAttribute("Note")]
                public string UidNote { get; set; }

                [XmlElement("GUID", IsNullable = true)]
                public Guid? UidGuid { get; set; }

            }
            
            private string _address;
            [XmlElement("IP_Address")]
            public string Address {
                get => _address;
                set
                {
                    try
                    {
                        IPAddress.Parse(value);
                        _address = value;
                    }
                    catch (FormatException ipFormatException)
                    {
                        Debugger.Log(1, "IPAddress Format Exception", ipFormatException.Message);
                        throw;
                    }
                    catch (ArgumentNullException ipNullException)
                    {
                        Debugger.Log(1, "IPAddress Format Exception", ipNullException.Message);
                        throw;
                    }
                    catch (Exception e)
                    {
                        Debugger.Log(1,"Unknown Error, General Catch",e.Message + "\n\n" + e.Source);
                        throw;
                    }
                }
            }

            [XmlElement("Active", IsNullable = true)]
            public bool? Active { get; set; }

            [XmlElement("Port_No")]
            public int PortNum { get; set; }

            [XmlElement("TimeOut_s")]
            public int TimeOut { get; set; }

            [XmlElement("OPC_Tags")]
            public ReaderTags Tags { get; set; }

            public class ReaderTags
            {
                public class ReadTags
                {
                    [XmlAttribute("Note")]
                    public string Note { get; set; }

                    [XmlElement("ReadTag")]
                    public string ReadTag { get; set; }
                }

                public class WriteTags
                {
                    [XmlAttribute("Note")]
                    public string Note { get; set; }

                    [XmlElement("WriteTag")]
                    public string WriteTag { get; set; }
                }

                public class UidTags
                {
                    [XmlAttribute("Note")]
                    public string Note { get; set; }

                    [XmlElement("UUID")]
                    public string UuidTag { get; set; }

                }

                public class ErrorTags
                {
                    [XmlAttribute("Note")]
                    public string Note { get; set; }

                    [XmlElement("Error_Boolean")]
                    public string ErrorBool { get; set; }
                    [XmlElement("Error_String")]
                    public string ErrorString { get; set; }
                }
                [XmlElement("ReadTags")]
                public ReadTags ReaderTag { get; set; }
                [XmlElement("WriteTags",IsNullable = true)]
                public WriteTags WriterTag { get; set; }
                [XmlElement("UidTags")]
                public UidTags UuidTag { get; set; }
                [XmlElement("ErrorTags")]
                public ErrorTags ErrorTag { get; set; }

            }
        }
        public class OPC
        {
            [XmlElement("ServerAddress")]
            public string ServerAddress { get; set; }

            [XmlElement("ServerPort")]
            public int ServerPort { get; set; }

            [XmlElement("Authorization")]
            public LogonAuth Auth { get; set; }

            public class LogonAuth
            {
                [XmlAttribute("Note")]
                public string Note { get; set; }
                [XmlElement("AnonymousLogin", IsNullable = true)]
                public bool? AnonymousLogin { get; set; }

                [XmlElement("UserName", IsNullable = true)]
                public string Username { get; set; }

                [XmlElement("Password", IsNullable = true)]
                public string Password { get; set; }
            }

        }
    }
   

    

    
}
