using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using LogParsing.LogParsers;

namespace DataParallelismTask.LogParsers
{
    public class ParallelLogParser : ILogParser
    {
        private readonly FileInfo _file;
        private readonly Func<string, string?> _tryGetIdFromLine;

        public ParallelLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
        {
            _file = file;
            _tryGetIdFromLine = tryGetIdFromLine;
        }

        public string[] GetRequestedIdsFromLogFile()
        {
            var result = new ConcurrentBag<string>();
            Parallel.ForEach(File.ReadLines(_file.FullName), line => {
                var id = _tryGetIdFromLine(line);
                if (id != null)
                    result.Add(id);
            });
            return result.ToArray();
        }
    }
}