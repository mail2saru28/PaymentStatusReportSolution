using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentStatusReport
{
    public enum StatusTypeEnum
    {

        [Description("Accepted")]
        ACCP,
        [Description("Partially Accepted")]
        PART,
        [Description("Reject")]
        RJCT

    }
}
