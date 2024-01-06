
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Contract;
using System.Windows.Shapes;

namespace RectangleLib
{
    public class RectangleShape : IShape
    {
        public BitmapImage _brush = null;
        public override UIElement Draw(bool isSelectMode, bool isOnTopLayer)
        {
            // TODO: can dam bao Diem 0 < Diem 1
            double width = Points[1].X - Points[0].X;
            double height = Points[1].Y - Points[0].Y;

            var rectangle = new Rectangle()
            {
                Width = (int)Math.Abs(width),
                Height = (int)Math.Abs(height),

                StrokeThickness = Thickness,
                Stroke = new SolidColorBrush(Color),
                StrokeDashArray = StrokeType,
                Fill = _brush == null ? new SolidColorBrush(Fill) : new ImageBrush(_brush),
            };

            if (isSelectMode && isOnTopLayer)
            {
                rectangle.Cursor = Cursors.Hand;
                rectangle.MouseLeftButtonDown += ShapeSelected;
            }
            if (width > 0 && height > 0)
            {
                Canvas.SetLeft(rectangle, Points[0].X);
                Canvas.SetTop(rectangle, Points[0].Y);
            }
            if (width > 0 && height < 0)
            {
                Canvas.SetLeft(rectangle, Points[0].X);
                Canvas.SetTop(rectangle, Points[1].Y);
            }
            if (width < 0 && height > 0)
            {
                Canvas.SetLeft(rectangle, Points[1].X);
                Canvas.SetTop(rectangle, Points[0].Y);
            }
            if (width < 0 && height < 0)
            {
                Canvas.SetLeft(rectangle, Points[1].X);
                Canvas.SetTop(rectangle, Points[1].Y);
            }

            return rectangle;
        }
        public void ShapeSelected(object sender, MouseButtonEventArgs e)
        {
            IsSelected = true;
        }

        public override IShape Clone()
        {
            return new RectangleShape();
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
            var result = new RectangleShape();
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

        public override string Name => "Rectangle";
    }

}
