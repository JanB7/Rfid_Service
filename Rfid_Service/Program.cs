using System.Threading;

namespace Rfid_Service
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main()
        {
#if DEBUG
            var serviceToDebug = new RfidService();
            serviceToDebug.OnDebug();
            Thread.Sleep(Timeout.Infinite);
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