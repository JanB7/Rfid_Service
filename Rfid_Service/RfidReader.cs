using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReaderB;

namespace Rfid_Service
{
    class RfidReader : IDisposable
    {
        private readonly EventLog _eventLog;

        private int _readerRef = 0;
        private int _fCmdRet;

        private int _errorCode;

        private int _portHandle;

        private byte _comAddress = 0xff;

        public RfidReader(ServiceConfig.Reader readerConfig, ref EventLog log)
        {
            _eventLog = log;
            try
            {
                var connected = Connect(readerConfig.Address, readerConfig.PortNum);
                if (connected != 0x35 || connected != 0x00) //Success
                {
                    _eventLog.WriteEntry($"Unsuccessfully Connected\n{EventIds.GetReturnCodeDesc(connected)}", EventLogEntryType.FailureAudit,(int)EventIds.Ids.UnsuccessfulRFIDConnection);
                }
            }
            catch (Exception e)
            {
                _eventLog.WriteEntry($"Unknown Error\n{e.Message}\n{e.StackTrace}\n\n{e.InnerException}", EventLogEntryType.Error, (int)EventIds.Ids.UnknownError);

            }

        }

        public bool GetStatus()
        {
            var connected = (_portHandle == 0x35 || _portHandle == 0x00);

            return connected;
        }

        
        private int Connect(string ipAddress, int port)
        {
            _readerRef = StaticClassReaderB.OpenNetPort(port, ipAddress, ref _comAddress, ref _portHandle);
            Task.Delay(1000);
            return _portHandle;
        }

        ~RfidReader()
        {
            Dispose();
        }

        private static byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }

        private static string ByteArrayToHexString(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            return sb.ToString().ToUpper();
        }

        #region Private Read EPC

        /// <summary>
        /// Used to read an EPC with NO Lock on the system
        /// </summary>
        /// <returns></returns>
        public string Read_Epc()
        {
            var str = Inventory();
            if (str == null)
            {
                return null;
            }

           
            #region ReaderRequirements

            const byte wordPtr = 0;
            const byte mem = 1;
            byte[] cardData = new byte[320];

            byte wNum = Convert.ToByte(Convert.ToInt64(str[1]) - 2);
            byte epcLength = Convert.ToByte(str[0].Length / 2);
            byte eNum = Convert.ToByte(str[0].Length / 4);

            byte MaskFlag = 0, MaskAdd = 0, MaskLen = 0;
            var fPassWord = HexStringToByteArray("00000000");

            byte[] epc = new byte[eNum];
            epc = HexStringToByteArray(str[0]);

            #endregion ReaderRequirements

            _fCmdRet = StaticClassReaderB.ReadCard_G2(ref _comAddress, epc, mem, wordPtr, wNum, fPassWord, MaskAdd,
                MaskLen, MaskFlag, cardData, epcLength, ref _errorCode, _portHandle);

            if (_fCmdRet == 0) //Successful read
            {
                byte[] daw = new byte[wNum * 2];
                Array.Copy(cardData, daw, wNum * 2);


                return ByteArrayToHexString(daw);
            }

            if (_errorCode == -1) return null;
            _eventLog.WriteEntry(
                $"Error reading EPC Value. ErrorCode=0x{Convert.ToString(_errorCode, 2)}({EventIds.GetErrorCodeDesc(_errorCode)})",EventLogEntryType.Error,(int)EventIds.Ids.GetTagInventoryFailure);
            return null;
        }

        #endregion Private Read EPC

        #region Get Tags

        /// <summary>
        /// <para>Gets current tags within range and return the first tag. Inventory not kept.</para>
        /// <remarks>
        /// <para>
        /// Returns EPC of type <see cref="string"/>[2] || EPC[0] = Tag Lenght | EPC[1] = Tag ID
        /// </para>
        /// </remarks>
        /// </summary>
        /// <returns>[0] = Tag Lenght [1] = Tag ID</returns>
        private string[] Inventory()
        {
            #region ReaderInventoryReq

            byte AdrTID = 0;
            byte LenTID = 0;
            byte TIDFlag = 0;

            byte comAddrr = 0xff;

            byte[] EPC = new byte[5000];

            int Totallen = 0;
            int CardNum = 0;

            string[] fInventory_EPC_List = new string[2];

            #endregion ReaderInventoryReq

            _fCmdRet = StaticClassReaderB.Inventory_G2(ref comAddrr, AdrTID, LenTID,
                TIDFlag, EPC, ref Totallen, ref CardNum, _portHandle);

            if ((_fCmdRet == 1) | (_fCmdRet == 2) | (_fCmdRet == 3) | (_fCmdRet == 4)) //251 = no tags detected
            {
                byte[] daw = new byte[Totallen];
                Array.Copy(EPC, daw, Totallen);
                fInventory_EPC_List[0] = ByteArrayToHexString(daw).Remove(0, 2);
                fInventory_EPC_List[1] = ByteArrayToHexString(daw).Remove(2);
                _eventLog.WriteEntry("Inventory Gather status\n" + EventIds.GetErrorCodeDesc(_fCmdRet), EventLogEntryType.Information, (int)EventIds.Ids.GetTagInventorySuccess);
                return fInventory_EPC_List;
            }

            _eventLog.WriteEntry("Inventory Gather status\n" + EventIds.GetErrorCodeDesc(_fCmdRet), EventLogEntryType.Information, (int)EventIds.Ids.GetTagInventoryFailure);
            return null;
        }

        #endregion Get Tags

        #region Write EPC

        public string WriteEpc()
        {
                #region ReaderRequirements

                byte WordPtr = 1, ENum;
                byte Mem = 1;
                byte WNum = 0;
                byte EPClength = 0;
                byte Writedatalen = 0;
                int WrittenDataNum = 0;
                byte[] CardData = new byte[320];
                byte[] writedata = new byte[230];

                byte MaskFlag = 0, MaskAdd = 0, MaskLen = 0;

                var fPassword = HexStringToByteArray("00000000");

                #endregion ReaderRequirements

                var epcVal = Read_Epc();

                #region Setup GUID

                var guid = Guid.NewGuid().ToString().Replace("-", null).ToUpper(); //32 characters long
                guid = guid + epcVal.Substring(epcVal.Length - 4);
                ENum = Convert.ToByte(epcVal.Length / 4);
                EPClength = Convert.ToByte(ENum * 2);
                byte[] EPC = new byte[ENum];

                WNum = Convert.ToByte(guid.Length / 4);
                byte[] Writedata = new byte[WNum * 2];

                Writedata = HexStringToByteArray(guid);
                Writedatalen = Convert.ToByte(WNum * 2);

                #endregion Setup GUID

                _fCmdRet = StaticClassReaderB.WriteCard_G2(ref _comAddress, EPC, Mem, WordPtr,
                    Writedatalen, Writedata, fPassword, MaskAdd, MaskLen, MaskFlag, WrittenDataNum,
                    EPClength, ref _errorCode, _comAddress);

                return guid;
        }

        #endregion Write EPC

        public void Dispose()
        {
            StaticClassReaderB.CloseNetPort(_readerRef);
        }
    }
}
