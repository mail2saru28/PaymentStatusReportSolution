using PaymentStatusReport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Reader
{
    class Program
    {
        static void Main(string[] args)
        {

            string targetDirectory = @"C:\docs\folder\";
            try
            {
                PaymentStatusReader obj = new PaymentStatusReader();
                var doc = obj.GetPaymentStatusInfo(targetDirectory);

            }
            catch (Exception ex)
            {

            }
        }
    }
}
