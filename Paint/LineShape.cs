using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Windows.Input;

namespace Paint
{
    public class LineShape : IShape
    {
        public override UIElement Draw(bool isSelectMode, bool isOnTopLayer)
        {
            Line l = new Line()
            {
                X1 = Points[0].X,
                Y1 = Points[0].Y,
                X2 = Points[1].X,
                Y2 = Points[1].Y,
                Stroke = new SolidColorBrush(Color),
                StrokeThickness = Thickness,
                StrokeDashArray = StrokeType,
                Fill = new SolidColorBrush(Color),
            };

            if (isSelectMode && isOnTopLayer)
            {
                l.Cursor = Cursors.Hand;
                l.MouseLeftButtonDown += ShapeSelected;

            }
            return l;
        }

        public void ShapeSelected(object sender, MouseButtonEventArgs e)
        {
            IsSelected = true;
        }

        public override IShape Clone()
        {
            return new LineShape();
        }

        public override void WriteShapeToBinary(BinaryWriter w)
        {
            throw new NotImplementedException();
        }

        public override IShape ReadShapeFromBinary(BinaryReader r)
        {
            throw new NotImplementedException();
        }

        public override string Name => "Line";

    }
}
