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
using Path = System.IO.Path;
using Contract;
using RectangleLib;


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
            var window = Window.GetWindow(this);
            window.KeyDown += HandleKeyPressed;
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
        public bool isDelete = false;
        public static string FilePath = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        // Shape style
        public Color StrokeColor { get; set; }
        public Color FillColor { get; set; }

        // Zoom
        private int _startZoom = 0;
        private double _currentZoomPercent = 100;

        // Undo/Redo
        public StringBuilder hotkeyText = new StringBuilder();
        private List<IShape> redoIShape = new List<IShape>();

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
                var shape = item.Value;
                var button = new Fluent.ToggleButton()
                {
                    Content = shape.Name,
                    Header = shape.Name,
                    Tag = shape.Name,
                };

                button.Click += (sender, args) =>
                {
                    foreach (var control in ShapeGroupBox.Items)
                    {
                        ((Fluent.ToggleButton)control).IsChecked = false;
                    }
                    ((Fluent.ToggleButton)sender).IsChecked = true;
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
            if(isDelete)
            {
                MessageBox.Show("Please choose atleast 1 layer");
                return;
            }
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
            lowerLayersShapesCount = 0;

            layers[_currentLayer]._shapes = _shapes;
            //Duyệt xem layer nào được check thì vẽ
            for (int i = 0; i < layers.Count(); i++)
            {
                if (layers[i].isChecked)
                {
                    //if(i!=_currentLayer)
                    //{
                    //    lowerLayersShapesCount = lowerLayersShapesCount + layers[i]._shapes.Count;
                    //}
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
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //handle skip zoom start: one for onload - one for after loading, set sliderbar's value to 100

            if (_startZoom < 2)
            {
                _startZoom++;
                return;
            }

            //start zooming

            var currentZoomValue = (double)ZoomSlider.Value;
            var scale = new ScaleTransform();
            double percentage = (currentZoomValue / 100);

            DrawCanvas.RenderTransform = scale;            
            scale.ScaleX = percentage;
            scale.ScaleY = percentage;

            if (currentZoomValue < 100)
            {
                DrawCanvas.Height = this.ActualHeight - 170; //subtract 170 for ribbon height
                DrawCanvas.Width = this.ActualWidth;
            }

            else
            {
                DrawCanvas.Height = (this.ActualHeight - 170) * percentage; //subtract 170 for ribbon height
                DrawCanvas.Width = this.ActualWidth * percentage;
            }            

            //set zoom percent text

            _currentZoomPercent = currentZoomValue;
            Proportion.Text = $"{_currentZoomPercent}%";
        }

        private void HandleKeyPressed(object sender, KeyEventArgs e)
        {
            //đánh dấu đã xử lý sự kiện
            e.Handled = true;


            //kiểm tra nếu là phím hệ thống thì không xử lý
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LWin || key == Key.RWin) {
                return;
            }

            //tạo chuỗi tổ hợp phím
            hotkeyText = new StringBuilder();

            //thực hiện kiểm tra phím đầu tiên trong tổ hợp phím
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) {
                hotkeyText.Append("Ctrl");
            }

            //đưa phím thứ 2 vào chuỗi tổ hợp phím
            hotkeyText.Append(key.ToString());

            //thực hiện kiểm tra phím thứ 2 trong tổ hợp phím
            if (hotkeyText.ToString() == "CtrlZ") {
                Undo();
            }
            if (hotkeyText.ToString() == "CtrlY") {
                Redo();
            }

        }

        private void Undo()
        {
            //nếu shape vừa vẽ xong, chưa buông chuột thì buông chuột :0
            if (_selectedShapeIndex != null)
            {
                _shapes[_selectedShapeIndex.Value].IsSelected = false;
                _selectedShapeIndex = null;
            }

            //nếu ko có shape nào thì ko undo, return hàm
            if (_shapes.Count == 0) 
                return;

            //đưa shape sẽ undo vào redoIShape để sau này redo nếu cần, đồng thời xóa shape đó khỏi _shapes
            redoIShape.Add(_shapes[_shapes.Count - 1]);
            _shapes.RemoveAt(_shapes.Count - 1);

            //vẽ lại lên canvas
            ReDraw();
        }

        private void Redo()
        {   
            //nếu ko có shape nào thì ko redo, return hàm
            if (redoIShape.Count == 0) 
                return;

            //nếu shape vừa vẽ xong, chưa buông chuột thì buông chuột :0
            if (_selectedShapeIndex != null)
            {
                _shapes[_selectedShapeIndex.Value].IsSelected = false;
                _selectedShapeIndex = null;
            };

            //đưa shape sẽ redo vào _shapes, đồng thời xóa shape đó khỏi redoIShape (vì đã redo rồi)
            _shapes.Add(redoIShape[redoIShape.Count - 1]);
            redoIShape.RemoveAt(redoIShape.Count - 1);

            //vẽ lại lên canvas
            ReDraw();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Redo();
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
            saveFileDialog.Filter = "PNG Files (*.png)|*.png|Binary Files (*.bin)|*.bin";
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
            browseDialog.Filter = "PNG Files (*.png)|*.png|Binary Files (*.bin)|*.bin";
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddLayerBtn_Click(object sender, RoutedEventArgs e)
        {
            layers.Add(new Layer(layers.Count));
        }

        private void DeleteLayerBtn_Click(object sender, RoutedEventArgs e)
        {
            //Đảm bảo luôn có ít nhất 1 layer
            if (layers.Count == 1)
            {
                MessageBox.Show("Can not delete this layer, you need to keep atleast 1 layer");
                return;
            }

            if (ListViewLayers.SelectedItems.Count == 0)
                return;
            layers.RemoveAt(ListViewLayers.SelectedIndex);
            _currentLayer = 0;
            

            isDelete = true;
            OnLayersUpdatedDraw();
        }

        private void ListViewLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Check lúc xóa thì không có layer nào được chọn nên ListViewLayers.SelectedIndex=-1
            if (!layers.Any()) return;
            
            _currentLayer = ListViewLayers.SelectedIndex == -1 ? 0 : ListViewLayers.SelectedIndex;
            _shapes = layers[_currentLayer]._shapes;
            isDelete = false;
        }
        private void OnLayersUpdatedDraw()
        {
            if (_selectedShapeIndex is not null)
            {
                _selectedShapeIndex = null;
            }
            _cutSelectedShapeIndex = null;
            _copiedShape = null;
            ReDraw();
        }

    }
}