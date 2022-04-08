using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Newtonsoft.Json;

namespace Antyrama.Pinger.Converter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run);
        }

        private static void Run(Options options)
        {
            if (!string.IsNullOrEmpty(options.FileName))
            {
                ConvertFile(options.FileName, options);
            }
            
            if (!string.IsNullOrEmpty(options.LogsPath))
            {
                ConvertFiles(options.LogsPath, options);
            }
        }

        private static void ConvertFiles(string logsPath, Options options)
        {
            if (!Directory.Exists(logsPath))
            {
                Console.WriteLine($"Directory [{logsPath}] does not exists.");
                return;
            }

            var fileNames = Directory.EnumerateFiles(logsPath, "*.*");

            if (!fileNames.Any())
            {
                Console.WriteLine($"Directory [{logsPath}] looks empty.");
                return;
            }

            foreach (var fileName in fileNames)
            {
                ConvertFile(fileName, options);
            }
        }

        private static void ConvertFile(string fileName, Options options)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"File [{fileName}] does not exists.");
                return;
            }
            
            var path = Path.GetDirectoryName(Path.GetFullPath(fileName));
            var onlyFileName = Path.GetFileNameWithoutExtension(fileName);

            Console.WriteLine($"Converting [{fileName}] file ...");
            
            var logs = LoadLogs(fileName);

            var byTimestamp = logs.GroupBy(l => l.Properties.Timestamp);
            
            const int milsPerDay = 1000 * 60 * 60 * 24;

            var dateTime = DateTime.Now.Date;

            var dateTimes = Enumerable.Range(0, milsPerDay / options.Interval)
                .Select(i => dateTime.AddMilliseconds(i * options.Interval));

            var faulted = byTimestamp
                .Where(g => g.Count(y => y.Level == "Warning") == options.ServiceCount)
                .Select(g => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, g.Key.Hour, g.Key.Minute, g.Key.Second));
            
            var items =
                from time in dateTimes
                join fault in faulted on time equals fault into gj
                from leftTime in gj.DefaultIfEmpty()
                select new Item()
                {
                    Time = time,
                    IsFaulted = leftTime == time
                };

            var timeSpans = GetGaps(items);

            SaveSummary(path, onlyFileName, timeSpans);
            SaveResults(path, onlyFileName, items);
        }

        private static void SaveSummary(string path, string fileName, IEnumerable<TimeSpan> timeSpans)
        {
            var number = timeSpans.Count();
            var total = timeSpans.Sum(t => t.TotalMilliseconds) / 1000 / 60;
            var avg = timeSpans.Average(t => t.TotalMilliseconds) / 1000;
            var max = timeSpans.Max(t => t.TotalMilliseconds) / 1000;
            
            var outputFile = Path.Combine(path, string.Concat(fileName, ".summary.csv"));

            using var fileStream = File.Create(outputFile);
            using var streamWriter = new StreamWriter(fileStream);
            
            streamWriter.WriteLine("total down time [m], number of gaps, average gap [s], longest gap [s] ");
            streamWriter.WriteLine(string.Concat(total, ", ", number, ", ", avg, ", ", max));
            
            Console.WriteLine($"Summary saved to [{outputFile}]");
        }

        private static IEnumerable<TimeSpan> GetGaps(IEnumerable<Item> items)
        {
            var gaps = new List<TimeSpan>();

            var timeStart = DateTime.Now;
            var timeEnd = DateTime.Now;
            var isDown = false;
            foreach (var item in items)
            {
                if (item.IsFaulted && !isDown)
                {
                    timeStart = item.Time;
                    isDown = true;
                }

                if (!item.IsFaulted && isDown)
                {
                    timeEnd = item.Time;
                    isDown = false;
                    gaps.Add(timeEnd - timeStart);
                }
            }

            return gaps;
        }

        private static void SaveResults(string path, string fileName, IEnumerable<Item> times)
        {
            var outputFile = Path.Combine(path, string.Concat(fileName, ".csv"));

            using var fileStream = File.Create(outputFile);
            using var streamWriter = new StreamWriter(fileStream);
            
            streamWriter.WriteLine("time, isFaulted");
            streamWriter.WriteLine(string.Join(Environment.NewLine,
                times.Select(t => string.Concat(t.Time.ToString("HH:mm:ss"), ", ", t.IsFaulted ? 1 : 0))));
            
            Console.WriteLine($"Results saved to [{outputFile}]");
        }

        private static IEnumerable<LogEntry> LoadLogs(string fileName)
        {
            var result = new List<LogEntry>();
            using var fileStream = File.OpenRead(fileName);
            using var reader = new StreamReader(fileName);

            var lineNr = 0;
            while (reader.ReadLine() is { } line)
            {
                lineNr++;
                
                if (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        var entry = JsonConvert.DeserializeObject<LogEntry>(line);
                        if (entry?.Level == "Information")
                        {
                            continue;
                        }
                        
                        result.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Unable to deserialize line nr [{lineNr}]. See message below.");
                        Console.WriteLine(ex);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }

            return result;
        }
    }

    internal class Item
    {
        public DateTime Time { get; set; }
        public bool IsFaulted { get; set; }
    }
}
