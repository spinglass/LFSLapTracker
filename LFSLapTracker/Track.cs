using InSimDotNet.Helpers;
using InSimDotNet.Packets;
using LFSLapTracker.TrackPath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    class Track
    {
        public string ShortName { get { return m_ShortName; } }
        public string LongName { get { return m_LongName; } }
        public int NumNodes { get { return m_NumNodes; } }
        public int NumSectors { get { return m_Splits.Count + 1; } }

        public Track(IS_RST packet)
        {
            m_ShortName = packet.Track;
            m_LongName = TrackHelper.GetFullTrackName(packet.Track);
            m_NumNodes = packet.NumNodes;
            m_Finish = packet.Finish;
            m_Splits = new List<int>();
            if (packet.Split1 != UInt16.MaxValue)
            {
                m_Splits.Add(packet.Split1);
            }
            if (packet.Split2 != UInt16.MaxValue)
            {
                m_Splits.Add(packet.Split2);
            }
            if (packet.Split3 != UInt16.MaxValue)
            {
                m_Splits.Add(packet.Split3);
            }

            m_Path = new Path();
            m_Path.Load("Content/" + m_ShortName + ".pth");
        }

        public int GetNodeIndex(int nodeId)
        {
            if (m_NumNodes > 0)
            {
                int index = (nodeId - m_Finish + m_NumNodes) % m_NumNodes;
                return index;
            }
            return -1;
        }

        public Node GetNode(int nodeIndex)
        {
            return m_Path.Nodes[nodeIndex];
        }

        private string m_ShortName;
        private string m_LongName;
        private int m_NumNodes;
        private int m_Finish;
        private List<int> m_Splits;
        private Path m_Path;
    }
}
