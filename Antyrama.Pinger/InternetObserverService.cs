using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Timers;
using Serilog;

namespace Antyrama.Pinger
{
    public class InternetObserverService : IDisposable
    {
        private static readonly IDictionary<string, string> DefaultHosts = new Dictionary<string, string>()
        {
            { "Google", "8.8.8.8" },
            { "Level3", "4.2.2.2" },
            { "Cloudflare", "1.1.1.1" }
        };

        private readonly object _lock = new object();
        private readonly IGatewayInterfaceService _gatewayInterfaceService;
        private readonly Options _options;
        private readonly ILogger _logger;
        private readonly Timer _timer = new Timer();
        private readonly IDictionary<string, string> _hosts;

        public InternetObserverService(IGatewayInterfaceService gatewayInterfaceService, 
            Options options, ILogger logger)
        {
            _gatewayInterfaceService = gatewayInterfaceService;
            _options = options;
            _logger = logger;

            _hosts = ParseHosts(_options.Hosts);

            _timer.Interval = _options.Interval;
            _timer.Elapsed += (sender, eventArgs) => CallAll();
        }

        public void Start()
        {
            _timer.Start();
        }
        
        private void CallAll()
        {
            var logger = _logger.ForContext("Timestamp", DateTime.UtcNow);

            var errorCount = 0;

            Task.WaitAll(
                _hosts.Select(host =>
                        Task.Factory.StartNew(() => Call(host.Key, host.Value, _options.Interval, logger))
                            .ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    lock (_lock)
                                    {
                                        errorCount++;
                                    }
                                }
                            }))
                    .Union(new[]
                    {
                        Task.Factory.StartNew(() =>
                            CheckInterfaceConnectivity(logger))
                    })
                    .ToArray());

            if (errorCount == DefaultHosts.Count)
            {
                logger.Error("Ping to all services failed");
            }
        }

        private void CheckInterfaceConnectivity(ILogger logger)
        {
            try
            {
                var status = _gatewayInterfaceService.CheckInterface();
                if (status == 1)
                {
                    logger.Information($"Interface [{_options.InterfaceName}] status is [{status}] means connected");
                }
                else
                {
                    logger.Error($"Interface [{_options.InterfaceName}] status is [{status}] means disconnected");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to determine whether interface is operational");
            }
        }

        private static void Call(string name, string ip, int timeOut, ILogger logger)
        {
            using var ping = new Ping();
            var pingReply = ping.Send(ip, timeOut);

            if (!(pingReply is { Status: IPStatus.Success }))
            {
                logger.Warning(
                    $"Ping to [{name}, {ip}] failed with status [{pingReply?.Status}], took [{pingReply?.RoundtripTime} ms]");

                throw new InvalidOperationException();
            }

            logger.Information($"Ping to [{name}, {ip}] successful, took [{pingReply.RoundtripTime} ms]");
        }
        
        private static IDictionary<string, string> ParseHosts(IEnumerable<string> hosts)
        {
            if (hosts == null || !hosts.Any())
            {
                return DefaultHosts;
            }

            return hosts.Select(h => h.Split(':')).ToDictionary(k => k[0], v => v[1]);
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}