using System.Collections.Generic;

namespace Rfid_Service
{
    public class EventIds
    {
        public enum Ids
        {
            FileNotFound = 1000,
            IoError = 1001,
            EmptyFile = 1002,
            UnknownError = 2000,
            GetTagInventorySuccess = 1500,
            GetTagInventoryFailure = 1501,
            UnsuccessfulClose = 1502,
            OpcSuccessfulTagSubscription = 1700,
            OpcTagChange = 1701,
            OpcConnectionFailure = 1702,
            OpcTagNonExistent = 1703,
            OpcTagNotRecognized = 1704,
            OpcCertificateError = 1705,
            NullReaderError = 1250,
            UnsuccessfulRFIDConnection = 1251,

        }

        public static string GetReturnCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case 0x00:
                    return "Operation Succeeded";

                case 0x01:
                    return "Return before Inventory finished";

                case 0x02:
                    return "the Inventory-scan-time overflow";

                case 0x03:
                    return "More Data";

                case 0x04:
                    return "Reader module MCU is Full";

                case 0x05:
                    return "Access Password Error";

                case 0x09:
                    return "Destroy Password Error";

                case 0x0a:
                    return "Destroy Password Error Cannot be Zero";

                case 0x0b:
                    return "Tag Not Support the command";

                case 0x0c:
                    return "Use the command,Access Password Cannot be Zero";

                case 0x0d:
                    return "Tag is protected,cannot set it again";

                case 0x0e:
                    return "Tag is unprotected,no need to reset it";

                case 0x10:
                    return "There is some locked bytes,write fail";

                case 0x11:
                    return "can not lock it";

                case 0x12:
                    return "is locked,cannot lock it again";

                case 0x13:
                    return "Parameter Save Fail,Can Use Before Power";

                case 0x14:
                    return "Cannot adjust";

                case 0x15:
                    return "Return before Inventory finished";

                case 0x16:
                    return "Inventory-Scan-Time overflow";

                case 0x17:
                    return "More Data";

                case 0x18:
                    return "Reader module MCU is full";

                case 0x19:
                    return "Not Support Command Or AccessPassword Cannot be Zero";

                case 0xFA:
                    return "Get Tag,Poor Communication,Inoperable";

                case 0xFB:
                    return "No Tag Operable";

                case 0xFC:
                    return "Tag Return ErrorCode";

                case 0xFD:
                    return "Command length wrong";

                case 0xFE:
                    return "Illegal command";

                case 0xFF:
                    return "Parameter Error";

                case 0x30:
                    return "Communication error";

                case 0x31:
                    return "CRC checksum at error";

                case 0x32:
                    return "Return data length error";

                case 0x33:
                    return "Communication busy";

                case 0x34:
                    return "Busy,command is being executed";

                case 0x35:
                    return "ComPort Opened";

                case 0x36:
                    return "ComPort Closed";

                case 0x37:
                    return "Invalid Handle";

                case 0x38:
                    return "Invalid Port";

                case 0xEE:
                    return "Return command error";

                default:
                    return "";
            }
        }

        public static string GetErrorCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case 0x00:
                    return "Other error";

                case 0x03:
                    return "Memory out or pc not support";

                case 0x04:
                    return "Memory Locked and unwritable";

                case 0x0b:
                    return "No Power,memory write operation cannot be executed";

                case 0x0f:
                    return "Not Special Error,tag not support special error code";

                default:
                    return "";
            }
        }
    }
}
