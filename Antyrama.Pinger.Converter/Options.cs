using CommandLine;

namespace Antyrama.Pinger.Converter
{
    internal class Options
    {
        [Option('p', "logs-path", Group = "Input options", Required = false,
            HelpText = "Folder path with log files. All files will be taken.")]
        public string LogsPath { get; set; }

        [Option('f', "file-name", Group = "Input options", Required = false,
            HelpText = "File name to convert.")]
        public string FileName { get; set; }

        [Option('n', "service-count", Required = false,
            HelpText = "Number of service called each session. Default is: 3.")]
        public int ServiceCount { get; set; } = 3;

        [Option('i', "interval", Required = false, HelpText = "Ping interval in milliseconds. Default is 2000.")]
        public int Interval { get; set; } = 2000;
    }
}