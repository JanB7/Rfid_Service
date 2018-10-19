using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Rfid_Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
#if DEBUG
            var serviceToDebug = new RfidService();
            serviceToDebug.OnDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
            var servicesToRun = new ServiceBase[]
            {
                new RfidService()
            };
            ServiceBase.Run(servicesToRun);
#endif
        }
    }
}
