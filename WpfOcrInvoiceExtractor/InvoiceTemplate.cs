using Intuit.Ipp.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace WpfOcrInvoiceExtractor
{
    public class InvoiceTemplate
    {
        public List<ImageRegion> ImageRegions;

        public Vendor Vendor;

        public Bitmap Display;
    }
}
