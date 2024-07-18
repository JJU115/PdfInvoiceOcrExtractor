using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WpfOcrInvoiceExtractor
{
    internal class ImageRegion
    {
        public BitmapImage Image { get; set; }
        public int index { get; set; }
        public ImageRegion(string fileName, int index) {
            Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
            this.Image = new BitmapImage(uri);
            this.Image.DecodePixelHeight = 100;
            this.index = index;
        }
    }
}
