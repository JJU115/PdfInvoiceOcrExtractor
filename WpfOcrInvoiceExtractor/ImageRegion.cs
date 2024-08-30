using System.Windows.Media.Imaging;

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

    internal class ImageRegion
    {
        public BitmapImage Image { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public BillTopic BillTopic { get; set; }
        public ImageRegion(string fileName, int index) {
            Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
            this.Image = new BitmapImage(uri);
            this.Name = "";
            this.Image.DecodePixelHeight = 100;
            this.Index = index;
        }
    }
}
