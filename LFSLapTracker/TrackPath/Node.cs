using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker.TrackPath
{
    class Node
    {
        public Point Centre { get { return m_Centre; } }
        public Vector Direction { get { return m_Direction; } }

        public Node(BinaryReader reader)
        {
            m_Centre.SerialiseFixedPoint(reader);
            m_Direction.SerialiseSingle(reader);
            m_LimitLeft = reader.ReadSingle();
            m_LimitRight = reader.ReadSingle();
            m_DriveLeft = reader.ReadSingle();
            m_DriveRight = reader.ReadSingle();        }

        Point m_Centre;
        Vector m_Direction;
        double m_LimitLeft;
        double m_LimitRight;
        double m_DriveLeft;
        double m_DriveRight;
    }
}
