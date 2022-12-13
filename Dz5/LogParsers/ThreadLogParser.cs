using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using LogParsing.LogParsers;

namespace DataParallelismTask.LogParsers
{
    public class ThreadLogParser : ILogParser
    {
        private readonly FileInfo _file;
        private readonly Func<string, string?> _tryGetIdFromLine;

        public ThreadLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
        {
            _file = file;
            _tryGetIdFromLine = tryGetIdFromLine;
        }

        public string[] GetRequestedIdsFromLogFile()
        {
            var linesList = File.ReadLines(_file.FullName).ToList();
            var threadsCount = Environment.ProcessorCount;
            var partSize = linesList.Count / threadsCount;
            var parts = new IEnumerable<string>[threadsCount];

            var partsList = linesList
                .Select((line, index) => new { Value = line, Index = index })
                .Where(line => line.Index / partSize < threadsCount)
                .GroupBy(line => line.Index / partSize)
                .Select(lineGroup => lineGroup.Select(line => line.Value)).ToList();
            
            var threads = new List<Thread>();
            for(var i = 0; i < linesList.Count % threadsCount; i++) 
                partsList[i] = partsList[i].Append(linesList[linesList.Count - i - 1]);
            
            var currentPartIndex = 0;
            foreach (var part in partsList)
            {
                var index = currentPartIndex;
                var thread = new Thread(() => Parsing(index, part, parts));
                threads.Add(thread);
                currentPartIndex++;
            }

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();
            
            return parts.SelectMany(part => part).ToArray();
        }

        [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: System.Int32[]; size: 115MB")]
        private void Parsing(int index, IEnumerable<string> linesToParse, IList<IEnumerable<string>> parts)
        {
            parts[index] = linesToParse
                .Select(_tryGetIdFromLine)
                .Where(id => id != null)
                .ToArray()!;
        }
    }
}