using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentStatusReport
{
    public class Account
    {
        public string Name { get; set; } //<Cdtr>
        public string IBAN { get; set; }
        public string OtherInformationId { get; set; }
        public string CodeORProprietary { get; set; }

    }
}
