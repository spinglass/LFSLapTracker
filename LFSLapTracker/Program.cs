using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine commandLine = new CommandLine(args);
            InSimConnection m_InSimConnection = new InSimConnection(commandLine);
            m_InSimConnection.Run();
        }
    }
}
