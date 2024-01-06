using Contract;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paint
{
    public class Layer : INotifyPropertyChanged
    {
        public bool isChecked { get; set; }
        public int index { get; set; }
        public List<IShape> _shapes { get; set; }
        public Layer(int index, bool isChecked = false)
        {
            this.index = index;
            this.isChecked = isChecked;
            _shapes = new List<IShape>();
        }

        public Layer()
        {
            _shapes = new List<IShape>();
        }

        void WriteLayerBinary(BinaryWriter bw)
        {
            bw.Write(isChecked);
            bw.Write(index);
            bw.Write(_shapes.Count);
            foreach (var shape in _shapes)
            {
                shape.WriteShapeToBinary(bw);
            }
        }

        public static void WriteLayerListBinary(BinaryWriter bw, List<Layer> writeDownList)
        {
            bw.Write(writeDownList.Count);
            foreach (var writerDownLayer in writeDownList)
            {
                writerDownLayer.WriteLayerBinary(bw);
            }
        }
        static Layer ReadLayerBinary(BinaryReader br)
        {
            var result = new Layer();
            result.isChecked = br.ReadBoolean();
            result.index = br.ReadInt32();
            int numberOfShape = br.ReadInt32();
            for (int i = 0; i < numberOfShape; i++)
            {
                string shapeType = br.ReadString();
                result._shapes.Add(MainWindow._prototypes[shapeType].ReadShapeFromBinary(br));
            }
            return result;
        }

        public static List<Layer> ReadLayerListBinary(BinaryReader br)
        {
            var result = new List<Layer>();
            var countShape = br.ReadInt32();
            for (int i = 0; i < countShape; i++)
            {
                result.Add(Layer.ReadLayerBinary(br));
            }
            return result;
        }
        public event PropertyChangedEventHandler? PropertyChanged = null;
    }
}
