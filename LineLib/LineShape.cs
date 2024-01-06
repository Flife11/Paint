
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using Contract;

namespace LineLib
{
    public class LineShape : IShape
    {
        public override string Name => "Line";


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
        public LineShape()
        {
        }
        public override void WriteShapeToBinary(BinaryWriter w)
        {
            w.Write(Name);
            w.Write(Points[0].X);
            w.Write(Points[0].Y);
            w.Write(Points[1].X);
            w.Write(Points[1].Y);
            w.Write(Thickness);
            w.Write(IsSelected);
            w.Write(Color.ToString());
            w.Write(Fill.ToString());
            w.Write(StrokeType.Count);
            foreach (var item in StrokeType)
            {
                w.Write(item);
            }
        }

        public override IShape ReadShapeFromBinary(BinaryReader r)
        {
            var result = new LineShape();
            PointP _start = new PointP();
            PointP _end = new PointP();
            _start.X = r.ReadDouble();
            _start.Y = r.ReadDouble();
            _end.X = r.ReadDouble();
            _end.Y = r.ReadDouble();
            result.Points.Add(_start);
            result.Points.Add(_end);
            result.Thickness = r.ReadInt32();
            result.IsSelected = r.ReadBoolean();
            var tempColor = r.ReadString();
            result.Color = (Color)ColorConverter.ConvertFromString(tempColor);
            var tempFill = r.ReadString();
            result.Fill = (Color)ColorConverter.ConvertFromString(tempFill);
            var count = r.ReadInt32();
            result.StrokeType = new DoubleCollection();
            for (int i = 0; i < count; i++)
            {
                result.StrokeType.Add(r.ReadDouble());
            }
            return result;
        }

    }
}
