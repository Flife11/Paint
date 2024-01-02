using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.IO;
using System.Windows.Input;

namespace Paint
{
    public class ElipseShape : IShape
    {
        public override string Name => "Elipse";

        public ElipseShape()
        {                       
        }        

        public override IShape Clone()
        {
            return new ElipseShape();
        }

        public override UIElement Draw(bool isSelectMode, bool isOnTopLayer)
        {
            double width = Points[1].X - Points[0].X;
            double height = Points[1].Y - Points[0].Y;

            var ellipse = new Ellipse()
            {
                Width = (int)Math.Abs(width),
                Height = (int)Math.Abs(height),
                StrokeThickness = Thickness,
                Stroke = new SolidColorBrush(Color),
                StrokeDashArray = StrokeType,
                Fill = new SolidColorBrush(Fill),

            };

            if (isSelectMode && isOnTopLayer)
            {
                ellipse.Cursor = Cursors.Hand;
                ellipse.MouseLeftButtonDown += ShapeSelected;
            }
            if (width > 0 && height > 0)
            {
                Canvas.SetLeft(ellipse, Points[0].X);
                Canvas.SetTop(ellipse, Points[0].Y);
            }
            if (width > 0 && height < 0)
            {
                Canvas.SetLeft(ellipse, Points[0].X);
                Canvas.SetTop(ellipse, Points[1].Y);
            }
            if (width < 0 && height > 0)
            {
                Canvas.SetLeft(ellipse, Points[1].X);
                Canvas.SetTop(ellipse, Points[0].Y);
            }
            if (width < 0 && height < 0)
            {
                Canvas.SetLeft(ellipse, Points[1].X);
                Canvas.SetTop(ellipse, Points[1].Y);
            }

            return ellipse;
        }

        public void ShapeSelected(object sender, MouseButtonEventArgs e)
        {
            IsSelected = true;
        }

        public override IShape ReadShapeFromBinary(BinaryReader r)
        {
            var result = new ElipseShape();
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
    }
}
