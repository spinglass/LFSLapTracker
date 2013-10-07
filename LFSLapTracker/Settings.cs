using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    class Settings
    {
        public string LFS { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public bool UseAI { get; private set; }

        public Settings(CommandLine commandLine)
        {
            LFS = commandLine.GetValue("--lfs", "");
            Host = commandLine.GetValue("--host", "127.0.0.1");
            Port = commandLine.GetValue("--port", 29999);
            UseAI = commandLine.Contains("--ai");
        }
    }
}
