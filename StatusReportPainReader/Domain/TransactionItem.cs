using System.Collections.Generic;

namespace PaymentStatusReport
{
    public class TransactionItem
    {
        public Account CreditorAccount { get; set; }
        public Account DebitorAccount { get; set; }
        public Agent CreditorAgent { get; set; }
        public Agent DebitorAgent { get; set; }
        public string TransactionStatus { get; set; }
        public decimal Amount { get; set; }
        public string OriginalInstructionIdentification { get; set; }
        public string OriginalEndToEndIdentification { get; set; }
        public List<string> TransactionStatusReasonInfo{ get; set; }
    }
}