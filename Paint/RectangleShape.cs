using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.IO;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Paint
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
                Fill =_brush==null?  new SolidColorBrush(Fill) : new ImageBrush(_brush),
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
            throw new NotImplementedException();
        }

        public override IShape ReadShapeFromBinary(BinaryReader r)
        {
            throw new NotImplementedException();
        }

        public override string Name => "Rectangle";
    }
}
