using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace WpfOcrInvoiceExtractor
{
    public enum BillTopic
    {
        Amount,
        BillDate,
        BillNumber,
        Category,
        Class,
        Description,
        SalesTax,
        Vendor
    }

    public class ImageRegion
    {
        [XmlIgnore]
        public CroppedBitmap Image { get; set; }
        [XmlIgnore]
        public BitmapImage DebugImage { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public BillTopic BillTopic { get; set; }

        public ImageRegion() { }

        public ImageRegion(string fileName, int index) {
            Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
            this.DebugImage = new BitmapImage(uri);
            this.Name = "";
            this.DebugImage.DecodePixelHeight = 100;
            this.Index = index;
        }
    }
}
