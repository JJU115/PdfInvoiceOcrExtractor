using Intuit.Ipp.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace WpfOcrInvoiceExtractor
{
    public class InvoiceTemplate
    {
        public List<ImageRegion> ImageRegions { get; set; }

        public Vendor Vendor { get; set; }

        [XmlIgnore]
        public BitmapImage Display { get; set; }
    }
}
