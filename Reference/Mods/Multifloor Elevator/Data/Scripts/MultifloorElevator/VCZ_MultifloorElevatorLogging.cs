using System;
using System.IO;
using System.Text;
using Sandbox.ModAPI;

namespace Vicizlat.MultifloorElevator
{
    internal class Logging
    {
        private static Logging _instance;
        private readonly StringBuilder _cache = new StringBuilder();
        private readonly TextWriter _writer;

        public Logging(string logFile)
        {
            _writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(logFile, typeof(Logging));
            _instance = this;
        }

        public static Logging Instance
        {
            get
            {
                if (MyAPIGateway.Utilities == null)
                {
                    return null;
                }
                if (_instance == null)
                {
                    _instance = new Logging("MultifloorElevator.log");
                }
                return _instance;
            }
        }

        public void WriteLine(string text)
        {
            try
            {
                if (_cache.Length > 0)
                {
                    _writer.WriteLine(_cache);
                }
                _cache.Clear();
                _cache.Append(DateTime.Now.ToString("[HH:mm:ss:ffff] "));
                _writer.WriteLine(_cache.Append(text));
                _writer.Flush();
                _cache.Clear();
            }
            catch
            {
                //logger failed, all hope is lost     
            }
        }

        internal void Close()
        {
            if (_cache.Length > 0)
            {
                _writer.WriteLine(_cache);
            }
            _writer.Flush();
            _writer.Close();
        }
    }
}