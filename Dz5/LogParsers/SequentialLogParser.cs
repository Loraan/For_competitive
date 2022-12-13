using System;
using System.IO;
using System.Linq;

namespace LogParsing.LogParsers
{
    public class SequentialLogParser : ILogParser 
    {
        private readonly FileInfo _file;
        private readonly Func<string, string?> _tryGetIdFromLine;

        public SequentialLogParser(FileInfo file, Func<string, string?> tryGetIdFromLine)
        {
            this._file = file;
            this._tryGetIdFromLine = tryGetIdFromLine;
        }
        
        public string[] GetRequestedIdsFromLogFile()
        {
            var lines = File.ReadLines(_file.FullName);
            return lines
                .Select(_tryGetIdFromLine)
                .Where(id => id != null)
                .ToArray();
        }
    }
}