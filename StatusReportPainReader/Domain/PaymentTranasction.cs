using System.Collections.Generic;

namespace PaymentStatusReport
{
    public class PaymentTransaction
    {
       public PaymentTransaction()
        {
            TransactionItems = new List<TransactionItem>();
        } 
        public string PaymentId { get; set; }
        public List<TransactionItem>  TransactionItems { get; set; }
       

    }
}