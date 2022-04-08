using Serilog;
using Serilog.Formatting.Json;
using System;
using System.IO;
using System.Linq;
using CommandLine;

#if DEBUG
#else
using System.Threading;
#endif

namespace Antyrama.Pinger
{
    internal class Program
    {
        private const string LogsFileNamePattern = "log.json";
        
#if DEBUG
#else
        private static ManualResetEvent _neverQuitEvent = new ManualResetEvent(false);
#endif
        
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run);
        }
        
        private static void Run(Options options)
        {
            ConfigureLogging(options);

            var gatewayInterfaceService = new GatewayInterfaceService(options);

            if (options.ListInterfaces)
            {
                ListInterfaces(gatewayInterfaceService, options);
                return;
            }

            using var service = new InternetObserverService(gatewayInterfaceService, options, Log.Logger);
            service.Start();
#if DEBUG
            Console.ReadKey();
#else
            _neverQuitEvent.WaitOne();
#endif
            Log.CloseAndFlush();
        }

        private static void ConfigureLogging(Options options)
        {
            var logsFilePath = string.IsNullOrEmpty(options.LogsPath)
                ? LogsFileNamePattern
                : Path.Combine(options.LogsPath, LogsFileNamePattern);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .WriteTo.File(new JsonFormatter(), logsFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Logger.Information($"Logs file path is [{logsFilePath}]");
        }

        private static void ListInterfaces(GatewayInterfaceService service, Options options)
        {
            try
            {
                var interfaces = service.GetInterfaces();
                if (!interfaces.Any())
                {
                    Console.WriteLine($"No active interfaces found on your device at [{options.IpAddress}].");
                }
                
                Console.WriteLine($"Interfaces found at [{options.IpAddress}]");
                foreach (var @interface in interfaces)
                {
                    Console.WriteLine(@interface.Name);
                }

                Console.WriteLine();
                Console.WriteLine("Make sure you choose correct one which is an active gateway to your ISP.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Something went wrong when fetching interfaces. Check the message below. Make sure that given IP address [{options.IpAddress}] and OID [{options.DefaultOid}] are correct for your device.");
                Console.WriteLine();
                Console.WriteLine(ex);
            }
        }
    }
}
