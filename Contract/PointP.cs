
using System.Windows;

namespace Contract
{
    public class PointP
    {
        public double X { get; set; }
        public double Y { get; set; }

        public PointP()
        {
        }

        public PointP(double x, double y)
        {
            X = x;
            Y = y;
        }
        public PointP(Point p)
        {
            X = p.X;
            Y = p.Y;
        }
    }
}
