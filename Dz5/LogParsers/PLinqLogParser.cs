using System;
using System.IO;
using System.Linq;

namespace LogParsing.LogParsers
{
    public class PLinqLogParser : ILogParser
    {
        private readonly FileInfo _file;
        private readonly Func<string, string?> _tryGetIdFromLine;

        public PLinqLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
        {
            _file = file;
            _tryGetIdFromLine = tryGetIdFromLine;
        }

        public string[] GetRequestedIdsFromLogFile()
        {
            var file = File.ReadLines(_file.FullName);
            return file
                .AsParallel()
                .Select(_tryGetIdFromLine)
                .Where(s => s != null)
                .ToArray();
        }
    }
}