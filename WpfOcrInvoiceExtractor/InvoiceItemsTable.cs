using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfOcrInvoiceExtractor
{
    internal class InvoiceItemsTable
    {
        public double PreTaxSubtotal { get; set; }
        public bool IncludesGST {  get; set; }
        public double CalculatedGSTAmount { get; set; }
        public double SourceGSTAmount { get; set; }
        public bool IncludesPST { get; set; }
        public double CalculatedPSTAmount { get; set; }
        public double SourcePSTAmount { get; set; }
        public double FullTotalAmount { get; set; }



        public InvoiceItemsTable(bool PST, bool GST, double full) {
            PreTaxSubtotal = 0;
            IncludesGST = GST;
            IncludesPST = PST;
            FullTotalAmount = full;
            PreTaxSubtotal = 0;
        }


        public void AddItem(double cost)
        {
            PreTaxSubtotal += cost;
            CalculatedGSTAmount = Math.Round(PreTaxSubtotal * 0.05, 2);
            CalculatedPSTAmount = Math.Round(PreTaxSubtotal * 0.07, 2);
        }

        public bool TotalsMatch()
        {
            return PreTaxSubtotal + CalculatedGSTAmount + CalculatedPSTAmount == FullTotalAmount;
        }
    }
}
