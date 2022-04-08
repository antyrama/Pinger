using System.Collections.Generic;
using CommandLine;

namespace Antyrama.Pinger
{
    public class Options
    {
        [Option('i', "interval", Required = false, HelpText = "Ping interval in milliseconds. Default is 2000.")]
        public int Interval { get; set; } = 2000;

        [Option('p', "logs-path", Required = false,
            HelpText = "Path pointing where the log files will be written. Default is executable folder.")]
        public string LogsPath { get; set; }

        [Option('a', "ip-address", Required = false,
            HelpText = "An IP address of your router/gateway. Default is: 192.168.0.1")]
        public string IpAddress { get; set; } = "192.168.0.1";

        [Option('n', "interface-name", Required = false,
            HelpText = "Name of the interface which is connecting you to the world. List all your interfaces first, to get proper name, using another switch. Default is: ppp0")]
        public string InterfaceName { get; set; } = "ppp0";

        [Option('h', "host-names", Required = false,
            HelpText =
                "Host names or IP addresses with names. Default is Google:8.8.8.8, Level3:4.2.2.2, Cloudflare:1.1.1.1. Example usage: -h Google:8.8.8.8 \"Another host:1.2.3.4\"")]
        public IEnumerable<string> Hosts { get; set; }

        [Option('l', "list-interfaces", Required = false,
            HelpText = "Use to list all available interfaces. Remember to provide proper IP address of your router/gateway.")]
        public bool ListInterfaces { get; set; }

        [Option('o', "interfaces-oid", Required = false,
            HelpText =
                "Management Information Base (MIB) information/specifications from the device manufacturer for the corresponding network device, if default one is not working, list interfaces first and check with your device. https://cric.grenoble.cnrs.fr/Administrateurs/Outils/MIBS/?oid=1.3.6.1.2.1.2.2.1.2 Default is: 1.3.6.1.2.1.2.2.1.2")]
        public string DefaultOid { get; set; } = "1.3.6.1.2.1.2.2.1.2";
    }
}