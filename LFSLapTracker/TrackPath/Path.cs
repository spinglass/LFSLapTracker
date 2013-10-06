using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker.TrackPath
{
    class Path
    {
        public IList<Node> Nodes { get { return m_Nodes; } }

        public void Load(string filename)
        {
            using (StreamReader stream = new StreamReader(filename))
            {
                BinaryReader reader = new BinaryReader(stream.BaseStream);
                string tag = new string(reader.ReadChars(6));
                byte version = reader.ReadByte();
                byte revision = reader.ReadByte();

                if (tag == "LFSPTH" && version == 0 && revision == 0)
                {
                    int numNodes = reader.ReadInt32();
                    int finishLine = reader.ReadInt32();

                    m_Nodes = new Node[numNodes];
                    for (int i = 0; i < numNodes; ++i)
                    {
                        // Load nodes so finish line is node 0
                        int nodeIndex = (i - finishLine + numNodes) % numNodes;
                        m_Nodes[nodeIndex] = new Node(reader);
                    }
                }
            }
        }

        private Node[] m_Nodes;
    }
}
