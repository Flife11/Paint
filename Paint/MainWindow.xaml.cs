using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Fluent;
using Microsoft.Win32;

namespace Paint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {        
        List<DoubleCollection> StrokeTypes = new List<DoubleCollection>() { new DoubleCollection() { 1, 0 }, new DoubleCollection() { 6, 1 }, new DoubleCollection() { 1 }, new DoubleCollection() { 6, 1, 1, 1 } };        
        public MainWindow()
        {
            InitializeComponent();
        }
        private System.Windows.Controls.Image SelectedImage;
        bool _isDrawing = false;
        PointP _start = new PointP();
        PointP _end = new PointP();
        string _choice = "";
        IShape _preview;
        List<IShape> _shapes = new List<IShape>();
        string _seletedPrototypeName = "";
        public static Dictionary<string, IShape> _prototypes = new Dictionary<string, IShape>();
        //select, cut, copy, paste
        private int? _selectedShapeIndex;
        private int? _cutSelectedShapeIndex;
        private IShape _copiedShape;
        //Layer
        BindingList<Layer> layers = new BindingList<Layer>() { new Layer(0, true) };
        public int _currentLayer = 0;
        public int lowerLayersShapesCount = 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Shape style
        public Color StrokeColor { get; set; }
        public Color FillColor { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            StrokeColor = Colors.Black;
            FillColor = Colors.Transparent;


            //set datacontext cho binding
            this.DataContext = this;
            ListViewLayers.ItemsSource = layers;

            var abilities = new List<IShape>();

            // Do tim cac kha nang
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            var fis = (new DirectoryInfo(folder)).GetFiles("*.dll");

            foreach (var fi in fis)
            {
                var assembly = Assembly.LoadFrom(fi.FullName);
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    if (type.IsClass & (!type.IsAbstract))
                    {
                        if (typeof(IShape).IsAssignableFrom(type))
                        {
                            var shape = Activator.CreateInstance(type) as IShape;
                            _prototypes.Add(shape.Name, shape);
                        }
                    }
                }
            }

            // Tạo ra các nút bấm hàng mẫu
            foreach (var item in _prototypes)
            {
                var shape = item.Value as IShape;
                var button = new Fluent.ToggleButton()
                {
                    //Width = 80,
                    //Height = 35,
                    Content = shape.Name,
                    Header = shape.Name,
                    //SizeDefinition = "Small",
                    //GroupName = "Shape",
                    Tag = shape.Name,
                };

                button.Click += (sender, args) =>
                {
                    _seletedPrototypeName = (string)((Fluent.ToggleButton)sender).Tag;
                    _preview = _prototypes[_seletedPrototypeName].Clone();
                    SelectButton.IsChecked = false;
                };

                ShapeGroupBox.Items.Add(button);
            };                      

            if (_prototypes.Count > 0)
            {
                ((Fluent.ToggleButton)ShapeGroupBox.Items[0]).IsChecked = true;
                _seletedPrototypeName = _prototypes.First().Value.Name;
                _preview = _prototypes[_seletedPrototypeName].Clone();
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentLayer == -1)
            {
                MessageBox.Show("Please choose atleast 1 layer");
                return;
            }
            //Kiểm tra chọn layer nhưng layer đang bị ẩn(icon closed eye)
            else if (!layers[_currentLayer].isChecked)
            {
                MessageBox.Show("Please display selected layer for drawing");
                return;
            }

            _isDrawing = true;

            PointP pos = new PointP(e.GetPosition(DrawCanvas));

            //_preview.Points.Add(pos);
            _start = pos;

            //Set stroke properties
            _preview.Color = StrokeColor;
            _preview.Thickness = (int)buttonStrokeSize.Value;
            _preview.StrokeType = StrokeTypes[buttonStrokeType.SelectedIndex];
            _preview.Fill = FillColor;
            e.Handled = true;
        }

        private void ReDraw()//xóa và vẽ lại
        {
            DrawCanvas.Children.Clear();

            if (_currentLayer == -1)
                return;

            layers[_currentLayer]._shapes = _shapes;
            //Duyệt xem layer nào được check thì vẽ
            for (int i = 0; i < layers.Count(); i++)
            {
                if (layers[i].isChecked)
                {

                    foreach (var shape in layers[i]._shapes)
                    {
                        UIElement element = shape.Draw(SelectButton.IsChecked ?? false, i == _currentLayer);
                        DrawCanvas.Children.Add(element);
                        DrawCanvas.UpdateLayout();
                    }

                }
            }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                Point p = e.GetPosition(DrawCanvas);
                _end = new PointP(p.X, p.Y);
                _preview.Points.Clear();
                _preview.Points.Add(_start); _preview.Points.Add(_end);
                ReDraw();
                DrawCanvas.Children.Add(_preview.Draw(SelectButton.IsChecked ?? false, true));
            }
        }
        
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(DrawCanvas);
            _end = new PointP(p.X, p.Y);
            if (_isDrawing)
            {
                _isDrawing = false;
              
                _preview.Points.Clear();
                _preview.Points.Add(_start); _preview.Points.Add(_end);

                double previewSize = Math.Sqrt(Math.Pow((_preview.Points[1].X - _preview.Points[0].X), 2) +
                                               Math.Pow((_preview.Points[1].Y - _preview.Points[0].Y), 2));
                if (previewSize < 1)
                {
                    if (_selectedShapeIndex != null)
                    {
                        int index = _selectedShapeIndex.Value;
                        _shapes[index].IsSelected = false;

                        //remove adorner của shape khác
                        Adorner[] toRemoveArray =
                            AdornerLayer.GetAdornerLayer(DrawCanvas).GetAdorners(DrawCanvas.Children[lowerLayersShapesCount + index]);
                        if (toRemoveArray != null)
                        {
                            for (int x = 0; x < toRemoveArray.Length; x++)
                            {
                                AdornerLayer.GetAdornerLayer(DrawCanvas).Remove(toRemoveArray[x]);
                            }
                        }
                    };
                    _selectedShapeIndex = null;
                    return;
                };

                _shapes.Add(_preview);
                // Sinh ra đối tượng mẫu kế
                _preview = _prototypes[_seletedPrototypeName].Clone();

                ReDraw();
                int i = _shapes.Count - 1;
                _selectedShapeIndex = i;
                if (_shapes[i].Name != "Line")
                {
                    AdornerLayer.GetAdornerLayer(DrawCanvas.Children[lowerLayersShapesCount + i])
                        .Add(new ResizeShapeAdorner(DrawCanvas.Children[lowerLayersShapesCount + i], _shapes[i]));
                }
                else
                {
                    AdornerLayer.GetAdornerLayer(DrawCanvas.Children[lowerLayersShapesCount + i])
                        .Add(new ResizeLineAdorner(DrawCanvas.Children[lowerLayersShapesCount + i], _shapes[i]));
                }

            }
        }

        private void ZoomingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void OpenImageDialog_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                // Lấy đường dẫn tệp ảnh đã chọn
                string imagePath = openFileDialog.FileName;

                // Gọi hàm để vẽ ảnh lên Canvas
                DrawImageOnCanvas(imagePath,_start);
            }
        }
        private void DrawImageOnCanvas(string imagePath, PointP position)
        {
            try
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));

                Image imageControl = new Image();
                imageControl.Source = bitmapImage;

                imageControl.Width = bitmapImage.PixelWidth;
                imageControl.Height = bitmapImage.PixelHeight;

                // tao 1 RectangleShape chua thong tin cua Anh

                RectangleShape rectangleShape = new RectangleShape();
                rectangleShape.IsSelected = true;
                rectangleShape.Points.Add(position);
                PointP _p = new PointP();
                _p.X = position.X + imageControl.Width;
                _p.Y = position.Y + imageControl.Height;
                rectangleShape.Points.Add(_p);
                rectangleShape._brush = bitmapImage;
                rectangleShape.Color = Color.FromArgb(0,0,0, 0); ;
                rectangleShape.Thickness = 0;
                rectangleShape.StrokeType = StrokeTypes[buttonStrokeType.SelectedIndex];
                rectangleShape.Fill = FillColor;

                _shapes.Add(rectangleShape);
                //Canvas.SetLeft(rectangle, position.X);
                ReDraw();
                int i = _shapes.Count - 1;
                _selectedShapeIndex = i;
                if (_shapes[i].Name != "Line")
                {
                    AdornerLayer.GetAdornerLayer(DrawCanvas.Children[lowerLayersShapesCount + i])
                        .Add(new ResizeShapeAdorner(DrawCanvas.Children[lowerLayersShapesCount + i], _shapes[i]));
                }
                else
                {
                    AdornerLayer.GetAdornerLayer(DrawCanvas.Children[lowerLayersShapesCount + i])
                        .Add(new ResizeLineAdorner(DrawCanvas.Children[lowerLayersShapesCount + i], _shapes[i]));
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}