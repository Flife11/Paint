using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace Paint
{
    public class ResizeLineAdorner : Adorner
    {
        private IShape _shape;
        const double THUMB_SIZE = 5;
        private Thumb startThumb, endThumb;
        private Line selectedLine = new Line();
        private VisualCollection visualChildren;

        public ResizeLineAdorner(UIElement adornedElement, IShape shape) : base(adornedElement)
        {
            _shape = shape;
            visualChildren = new VisualCollection(this);

            startThumb = GetResizeThumb();
            endThumb = GetResizeThumb();

            startThumb.DragDelta += StartDragDelta;
            endThumb.DragDelta += EndDragDelta;

            visualChildren.Add(startThumb);
            visualChildren.Add(endThumb);

            selectedLine = (Line)AdornedElement;
        }

        private Thumb GetResizeThumb()
        {
            var thumb = new Thumb()
            {
                Width = THUMB_SIZE,
                Height = THUMB_SIZE,
                Cursor = Cursors.SizeAll,
                Template = new ControlTemplate(typeof(Thumb))
                {
                    VisualTree = GetThumbTemplate(new SolidColorBrush(Colors.White))
                }
            };
            return thumb;
        }
        private FrameworkElementFactory GetThumbTemplate(Brush back)
        {
            back.Opacity = 1;
            var fef = new FrameworkElementFactory(typeof(Rectangle));
            fef.SetValue(Rectangle.FillProperty, back);
            fef.SetValue(Rectangle.StrokeProperty, Brushes.SlateBlue);
            fef.SetValue(Rectangle.StrokeThicknessProperty, (double)1);
            return fef;
        }
        private void StartDragDelta(object sender, DragDeltaEventArgs e)
        {
            Point position = Mouse.GetPosition(this);

            selectedLine.X1 = position.X;
            selectedLine.Y1 = position.Y;
            _shape.Points[0].X = position.X;
            _shape.Points[0].Y = position.Y;
        }

        private void EndDragDelta(object sender, DragDeltaEventArgs e)
        {
            Point position = Mouse.GetPosition(this);

            selectedLine.X2 = position.X;
            selectedLine.Y2 = position.Y;
            _shape.Points[1].X = position.X;
            _shape.Points[1].Y = position.Y;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            selectedLine = (Line)AdornedElement;

            double left = Math.Min(selectedLine.X1, selectedLine.X2);
            double top = Math.Min(selectedLine.Y1, selectedLine.Y2);

            var startRect = new Rect(selectedLine.X1 - (startThumb.Width / 2), selectedLine.Y1 - (startThumb.Width / 2), startThumb.Width, startThumb.Height);
            startThumb.Arrange(startRect);

            var endRect = new Rect(selectedLine.X2 - (endThumb.Width / 2), selectedLine.Y2 - (endThumb.Height / 2), endThumb.Width, endThumb.Height);
            endThumb.Arrange(endRect);

            return finalSize;
        }

        protected override int VisualChildrenCount => visualChildren.Count;

        protected override Visual GetVisualChild(int index)
        {
            return visualChildren[index];

        }
    }
}
