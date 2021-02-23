using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArdNet.Server.Tests.CLI
{
    class Program
    {
        const bool DEBUG = false;

        static async Task<int> Main(string[] args)
        {
            var myProc = Process.GetCurrentProcess();
            myProc.PriorityClass = ProcessPriorityClass.RealTime;
            Console.SetIn(new StreamReader(Console.OpenStandardInput(8192), Encoding.ASCII, false, 8192));
            ThreadPool.SetMinThreads(4, 4);

            using (var server = new RobustServerCLI(DEBUG))
            {
                await server.RunServer();
            }

            return 0;
        }
    }
}
