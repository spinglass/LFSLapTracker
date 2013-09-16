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
            InSimConnection m_InSimConnection = new InSimConnection();
            m_InSimConnection.Run();
        }
    }
}
