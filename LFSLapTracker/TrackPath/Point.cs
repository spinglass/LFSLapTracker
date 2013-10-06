using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker.TrackPath
{
    struct Point
    {
        public double X;
        public double Y;
        public double Z;

        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector operator -(Point p1, Point p2)
        {
            return new Vector(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static Point operator +(Point p, Vector v)
        {
            return new Point(p.X + v.X, p.Y + v.Y, p.Z + v.Z);
        }

        public void SerialiseFixedPoint(BinaryReader reader)
        {
            X = FixedPointToDouble(reader);
            Y = FixedPointToDouble(reader);
            Z = FixedPointToDouble(reader);
        }

        private static double FixedPointToDouble(BinaryReader reader)
        {
            int fp = reader.ReadInt32();
            double d = (double)fp / 65536;
            return d;
        }
    }
}
