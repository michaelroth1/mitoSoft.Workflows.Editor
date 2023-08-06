using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mitoSoft.Workflows.Advanced.Tests
{
    internal class Logger
    {
        static Logger()
        {
            _logger = new();
        }

        protected static readonly List<string> _logger;

        public static void Log(string text)
        {
            Debug.WriteLine(text);
            _logger.Add(text);
        }

        public static void Clear()
        {
            _logger.Clear();
        }

        internal static string ShowTrace()
        {
            return string.Join("->", _logger.ToArray());
        }
    }
}