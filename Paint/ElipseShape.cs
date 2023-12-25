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
            throw new NotImplementedException();
        }

        public override void WriteShapeToBinary(BinaryWriter w)
        {
            throw new NotImplementedException();
        }
    }
}
