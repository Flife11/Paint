using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Paint
{
    public abstract class IShape
    {
        public abstract string Name { get; }
        public bool IsSelected { get; set; }
        public List<PointP> Points { get; set; } = new List<PointP>();
        public Color Color { get; set; }
        public Color Fill { get; set; }
        public int Thickness { get; set; }      
        public DoubleCollection StrokeType { get; set; }

        public abstract UIElement Draw(bool isSelectMode, bool isOnTopLayer);
        public abstract IShape Clone();
        public abstract void WriteShapeToBinary(BinaryWriter w);
        public abstract IShape ReadShapeFromBinary(BinaryReader r);
    }
}
