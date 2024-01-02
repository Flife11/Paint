using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Fluent;
using Path = System.IO.Path;

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

        bool _isDrawing = false;
        PointP _start = new PointP();
        PointP _end = new PointP();
        string _choice = "";
        IShape _preview;
        List<IShape> _shapes = new List<IShape>();
        string _seletedPrototypeName = "";
        public static Dictionary<string, IShape> _prototypes = new Dictionary<string, IShape>();
        //current shape dùng cho undo và redo
        private List<IShape> currentIShape = new List<IShape>();
        //select, cut, copy, paste
        private int? _selectedShapeIndex;
        private int? _cutSelectedShapeIndex;
        private IShape _copiedShape;
        //Layer
        BindingList<Layer> layers = new BindingList<Layer>() { new Layer(0, true) };
        public int _currentLayer = 0;
        public int lowerLayersShapesCount = 0;
        public static string FilePath = "";

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

        //Hàm cho chức năng save và save as:
        void CreateBitmapFromVisual(Visual target, string filename, string filerType)   //Tạo ảnh nếu chọn file ảnh
        {
            if (target == null)
                return;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);

            RenderTargetBitmap rtb = new RenderTargetBitmap((Int32)bounds.Width, (Int32)bounds.Height, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();

            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(target);
                dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
            }

            rtb.Render(dv);
            switch (filerType)
            {
                case ".png":
                    PngBitmapEncoder png = new PngBitmapEncoder();

                    png.Frames.Add(BitmapFrame.Create(rtb));
                    using (Stream stm = File.Create(filename))
                    {
                        png.Save(stm);
                    }
                    break;
                case ".bmp":
                    BitmapEncoder bmp = new BmpBitmapEncoder();
                    bmp.Frames.Add(BitmapFrame.Create(rtb));
                    using (Stream stm = File.OpenWrite(filename))
                    {
                        bmp.Save(stm);
                    }

                    break;
                case ".jpg":
                    JpegBitmapEncoder jpg = new JpegBitmapEncoder();
                    jpg.Frames.Add(BitmapFrame.Create(rtb));
                    using (Stream stm = File.OpenWrite(filename))
                    {
                        jpg.Save(stm);
                    }
                    break;
            }
        }
        public void SaveNew()   //Tạo file binary nếu chọn file binary
        {
            using (var stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None))

            using (var bw = new BinaryWriter(stream))
            {
                Paint.Layer.WriteLayerListBinary(bw, layers.ToList());
            }
        }
        private void SaveAs()
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.DefaultExt = "png";
            saveFileDialog.Filter = "PNG Files (*.png)|*.png|BMP Files (*.bmp)|*.bmp|JPG Files (*.jpg)|*.jpg|Binary Files (*.bin)|*.bin";
            if (saveFileDialog.ShowDialog() == true)
            {
                FilePath = saveFileDialog.FileName;
                switch (saveFileDialog.FilterIndex)
                {
                    case 1:
                        {
                            CreateBitmapFromVisual(DrawCanvas, saveFileDialog.FileName, ".png");
                            break;
                        }
                    case 2:
                        {
                            CreateBitmapFromVisual(DrawCanvas, saveFileDialog.FileName, ".bmp");
                            break;
                        }
                    case 3:
                        {
                            CreateBitmapFromVisual(DrawCanvas, saveFileDialog.FileName, ".jpg");
                            break;
                        }
                    case 4:
                        {
                            SaveNew();
                            break;
                        }
                }
            }
        }
        private void Save()
        {
            if (FilePath == "")
            {
                //buttonSaveAs_Click(sender, e);
                SaveAs();
                return;
            }
            string ext = Path.GetExtension(FilePath);
            DrawCanvas.UpdateLayout();
            if (ext == ".bin")
            {
                SaveNew();
                return;
            }
            CreateBitmapFromVisual(DrawCanvas, FilePath, ext);
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void buttonSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private void OnLayersUpdated()
        {
            if (_selectedShapeIndex is not null)
            {
                _selectedShapeIndex = null;
            }
            lowerLayersShapesCount = 0;
            for (int k = 0; k < _currentLayer; k++)
            {
                if (layers[k].isChecked) lowerLayersShapesCount += layers[k]._shapes.Count;
            }
            _cutSelectedShapeIndex = null;
            _copiedShape = null;
            currentIShape.Clear();
            ReDraw();
        }

        private void buttonOpen_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog browseDialog = new Microsoft.Win32.OpenFileDialog();
            browseDialog.Filter = "PNG Files (*.png)|*.png|BMP Files (*.bmp)|*.bmp|JPG Files (*.jpg)|*.jpg|Binary Files (*.bin)|*.bin";
            browseDialog.FilterIndex = 1;
            browseDialog.Multiselect = false;
            if (browseDialog.ShowDialog() != true)
            {
                return;
            }
            FilePath = browseDialog.FileName;
            if (Path.GetExtension(FilePath) == ".bin")
            {
                using (var stream = File.OpenRead(FilePath))
                {
                    using (var br = new BinaryReader(stream))
                    {
                        layers.Clear();
                        var layerData = Paint.Layer.ReadLayerListBinary(br);
                        foreach (var data in layerData)
                        {
                            layers.Add(data);
                        }
                    }
                }

                //Tính lại current layer và gán _shape = _shape của currentlayer
                _currentLayer = 0;
                ListViewLayers.SelectedIndex = _currentLayer;
                _shapes = layers[_currentLayer]._shapes;
                OnLayersUpdated();

                return;
            }
            MemoryStream ms = new MemoryStream();
            BitmapImage bi = new BitmapImage();
            if (FilePath != null)
            {
                byte[] bytArray = File.ReadAllBytes(FilePath);
                ms.Write(bytArray, 0, bytArray.Length);
            }

            ms.Position = 0;
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            ImageBrush ib = new ImageBrush();
            ib.ImageSource = bi;
            DrawCanvas.Background = ib;
        }
    }
}