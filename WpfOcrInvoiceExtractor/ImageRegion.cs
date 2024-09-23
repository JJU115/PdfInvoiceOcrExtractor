using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace WpfOcrInvoiceExtractor
{
    public enum BillTopic
    {
        Unmarked,
        Amount,
        BillDate,
        BillNumber,
        Vendor,
        ItemsTable,
        Category,
        Class,
        Description,
        SalesTax,        
    }

    public class ImageRegion
    {
        [XmlIgnore]
        public CroppedBitmap Image { get; set; }

        public Int32Rect SourceRegion { get; set; }

        [XmlIgnore]
        public BitmapImage DebugImage { get; set; }
        public int Index { get; set; }
        public BillTopic BillTopic { get; set; } = BillTopic.Unmarked;

        public ImageRegion() { }

        public ImageRegion(string fileName, int index) {
            Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
            this.DebugImage = new BitmapImage(uri);
            this.DebugImage.DecodePixelHeight = 100;
            this.Index = index;
        }
    }
}
