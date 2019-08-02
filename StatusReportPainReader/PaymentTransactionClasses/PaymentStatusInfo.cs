using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentStatusReport
{
    public class PaymentStatusInfo
    {
        public string FileName { get; set; }
        public string GroupStatus { get; set; }
        public List<PaymentTransaction> PaymentTransactionsInfo { get; set; }
        public List<string> GroupStatusReasonInformation { get; set; }
        public string TotalNumberOfTransactions { get; set; }
        public string TotalNumberOfAcceptedTransactions { get; set; }
        public string TotalNumberOfRejectedTransactions { get; set; }

    }
}
