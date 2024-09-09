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
        public BitmapImage Image { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public BillTopic BillTopic { get; set; }

        public ImageRegion() { }

        public ImageRegion(string fileName, int index) {
            Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
            this.Image = new BitmapImage(uri);
            this.Name = "";
            this.Image.DecodePixelHeight = 100;
            this.Index = index;
        }
    }
}
