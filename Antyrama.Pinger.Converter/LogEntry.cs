using System;

namespace Antyrama.Pinger.Converter
{
    internal class LogEntry
    {
        public string Level { get; set; }
        
        public Properties Properties { get; set; }
    }

    internal class Properties
    {
        public DateTime Timestamp { get; set; }
    }
}