using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PaymentStatusReport
{
    public class PaymentStatusReader
    {
        public List<PaymentStatusInfo> GetPaymentStatusInfo(string targetDirectory)
        {
            List<PaymentStatusInfo> paymentStatusInfoList = new List<PaymentStatusInfo>();
            try
            {
                string[] files = Directory.GetFiles(targetDirectory, "pain.002.001.03*.xml ");
                if (files.Count() > 0)
                {
                    foreach (string fileName in files)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
                            if (!File.Exists(fileName)) throw new FileNotFoundException("Input file not found", fileName);

                            Document doc = null;
                            using (Stream reader = new FileStream(fileName, FileMode.Open))
                            {
                                var serializer = new XmlSerializer(typeof(Document));
                                doc = (Document)serializer.Deserialize(reader);
                                var paymentStatusInfo = new PaymentStatusInfo();
                                paymentStatusInfo = GetPaymentStatusReport(doc, fileName);
                                paymentStatusInfoList.Add(paymentStatusInfo);
                            }
                        }
                        catch (Exception ex)
                        {
                            SaveLogFile(MethodBase.GetCurrentMethod(), ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SaveLogFile(MethodBase.GetCurrentMethod(), ex);
            }
            return paymentStatusInfoList;
        }
        private PaymentStatusInfo GetPaymentStatusReport(Document document, string fileName)
        {
            PaymentStatusInfo statusReport = new PaymentStatusInfo();
            try
            {
                var originalGrpInfoAndSts = document.CstmrPmtStsRpt.OrgnlGrpInfAndSts;
                List<string> groupStatusReason = new List<string>();
                statusReport.FileName = fileName;
                statusReport.GroupStatus = GetStatusType(originalGrpInfoAndSts.GrpSts.ToString());
                var groupStatusInfo = originalGrpInfoAndSts.StsRsnInf;
                if (groupStatusInfo != null)
                {
                    foreach (var groupStatus in groupStatusInfo)
                    {
                        var statusCode = groupStatus.Rsn.Item;
                        var additionalInfo = groupStatus.AddtlInf;
                        groupStatusReason.AddRange(additionalInfo);
                        groupStatusReason.Add(statusCode);
                    }
                }
                statusReport.GroupStatusReasonInformation = groupStatusReason;
                statusReport.TotalNumberOfTransactions = originalGrpInfoAndSts.OrgnlNbOfTxs;
                var numberOfTxnsPerStatus = originalGrpInfoAndSts.NbOfTxsPerSts;
                if (numberOfTxnsPerStatus?.Count() > 0)
                {
                    foreach (var txnPerSts in numberOfTxnsPerStatus)
                    {
                        if (txnPerSts.DtldSts.ToString() == StatusTypeEnum.ACCP.ToString())
                            statusReport.TotalNumberOfAcceptedTransactions = txnPerSts.DtldNbOfTxs;
                        if (txnPerSts.DtldSts.ToString() == StatusTypeEnum.RJCT.ToString())
                            statusReport.TotalNumberOfRejectedTransactions = txnPerSts.DtldNbOfTxs;
                    }

                }
                statusReport.PaymentTransactionsInfo = GetPaymentTransaction(document.CstmrPmtStsRpt.OrgnlPmtInfAndSts);
            }
            catch (Exception ex)
            {
                SaveLogFile(MethodBase.GetCurrentMethod(), ex);
            }

            return statusReport;
        }
        private List<PaymentTransaction> GetPaymentTransaction(OriginalPaymentInformation1[] OrgnlPmtInfAndSts)
        {
            List<PaymentTransaction> lstPaymentTransactions = new List<PaymentTransaction>();
            try
            {
                if (OrgnlPmtInfAndSts?.Count() > 0)
                {
                    foreach (var pmtInfo in OrgnlPmtInfAndSts)
                    {
                        PaymentTransaction paymentTranasction = new PaymentTransaction();
                        paymentTranasction.PaymentId = pmtInfo.OrgnlPmtInfId;

                        if (pmtInfo.TxInfAndSts?.Count() > 0)
                        {
                            paymentTranasction.TransactionItems = new List<TransactionItem>();
                            foreach (var txnInfo in pmtInfo.TxInfAndSts)
                            {
                                var transactionStatusReasonInfo = txnInfo.StsRsnInf;
                                List<string> transactionStatusRsn = new List<string>();
                                if (transactionStatusReasonInfo != null)
                                {
                                    foreach (var transactionStatus in transactionStatusReasonInfo)
                                    {
                                        var statusCode = transactionStatus.Rsn.Item;
                                        var additionalInfo = transactionStatus.AddtlInf;
                                        transactionStatusRsn.AddRange(additionalInfo);
                                        transactionStatusRsn.Add(statusCode);
                                    }
                                }
                                var transactionItem = new TransactionItem();
                                transactionItem.TransactionStatusReasonInfo = transactionStatusRsn;
                                transactionItem.TransactionStatus = GetStatusType(txnInfo.TxSts.ToString());
                                if (txnInfo.OrgnlTxRef != null)
                                {
                                    transactionItem.OriginalInstructionIdentification = txnInfo.OrgnlInstrId;
                                    transactionItem.OriginalEndToEndIdentification = txnInfo.OrgnlEndToEndId;
                                    var orgnlTxRefAmt = txnInfo.OrgnlTxRef.Amt?.Item;
                                    if (orgnlTxRefAmt.GetType() == typeof(EquivalentAmount2))
                                    {
                                        var amount = (EquivalentAmount2)orgnlTxRefAmt;
                                        transactionItem.Amount = amount.Amt.Value;
                                    }
                                    else if (orgnlTxRefAmt.GetType() == typeof(ActiveOrHistoricCurrencyAndAmount))
                                    {
                                        var amount = (ActiveOrHistoricCurrencyAndAmount)orgnlTxRefAmt;
                                        transactionItem.Amount = amount.Value;
                                    }
                                    #region Creditor Account And Agent Info
                                    transactionItem.CreditorAccount = new Account();
                                    transactionItem.CreditorAccount.Name = txnInfo.OrgnlTxRef.Cdtr?.Nm;
                                    if (txnInfo.OrgnlTxRef.CdtrAcct != null)
                                    {
                                        var cdtrAccountInfo = txnInfo.OrgnlTxRef.CdtrAcct?.Id;
                                        if (cdtrAccountInfo != null)
                                        {
                                            if (cdtrAccountInfo.Item.GetType() == typeof(GenericAccountIdentification1))
                                            {
                                                var cdtrAccount = (GenericAccountIdentification1)cdtrAccountInfo.Item;
                                                transactionItem.CreditorAccount.OtherInformationId = cdtrAccount.Id;
                                                transactionItem.CreditorAccount.CodeORProprietary = cdtrAccount.SchmeNm?.Item;
                                            }
                                            else
                                                transactionItem.CreditorAccount.IBAN = cdtrAccountInfo.Item?.ToString();
                                        }
                                    }

                                    transactionItem.CreditorAgent = new Agent();
                                    if (txnInfo.OrgnlTxRef.CdtrAgt?.FinInstnId != null)
                                    {
                                        var financialInstid = txnInfo.OrgnlTxRef.CdtrAgt.FinInstnId;
                                        transactionItem.CreditorAgent.BIC = financialInstid?.BIC;
                                        transactionItem.CreditorAgent.MemberIdentification = financialInstid.ClrSysMmbId?.MmbId;
                                        transactionItem.CreditorAgent.CodeORProprietary = financialInstid.ClrSysMmbId?.ClrSysId?.Item;
                                    }
                                    #endregion

                                    #region Debitor Account And Agent Info
                                    transactionItem.DebitorAccount = new Account();
                                    transactionItem.DebitorAccount.Name = txnInfo.OrgnlTxRef.Dbtr?.Nm;
                                    var dbtrAccountInfo = txnInfo.OrgnlTxRef.DbtrAcct?.Id;
                                    if (dbtrAccountInfo?.Item.GetType() == typeof(GenericAccountIdentification1))
                                    {
                                        var dbtrAccount = (GenericAccountIdentification1)dbtrAccountInfo.Item;
                                        transactionItem.DebitorAccount.OtherInformationId = dbtrAccount?.Id;
                                        transactionItem.DebitorAccount.CodeORProprietary = dbtrAccount.SchmeNm?.Item;
                                    }
                                    else
                                        transactionItem.DebitorAccount.IBAN = dbtrAccountInfo.Item.ToString();

                                    transactionItem.DebitorAgent = new Agent();
                                    transactionItem.DebitorAgent.BIC = txnInfo.OrgnlTxRef.DbtrAgt?.FinInstnId?.BIC;
                                    transactionItem.DebitorAgent.MemberIdentification = txnInfo.OrgnlTxRef.DbtrAgt?.FinInstnId?.ClrSysMmbId?.MmbId;
                                    transactionItem.DebitorAgent.CodeORProprietary = txnInfo.OrgnlTxRef.DbtrAgt?.FinInstnId?.ClrSysMmbId?.ClrSysId?.Item;
                                    #endregion

                                }
                                paymentTranasction.TransactionItems.Add(transactionItem);
                            }
                        }
                        lstPaymentTransactions.Add(paymentTranasction);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveLogFile(MethodBase.GetCurrentMethod(), ex);
            }
            return lstPaymentTransactions;
        }

        private string GetStatusType(string statusType)
        {
            string status = string.Empty;
            switch (statusType)
            {
                case "ACCP":
                    status = GetEnumDescription(StatusTypeEnum.ACCP);
                    break;
                case "PART":
                    status = GetEnumDescription(StatusTypeEnum.PART);
                    break;
                case "RJCT":
                    status = GetEnumDescription(StatusTypeEnum.RJCT);
                    break;
                default:
                    break;
            }
            return status;
        }
        private string GetEnumDescription(StatusTypeEnum value)
        {
            // Get the Description attribute value for the enum value
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }
        private void SaveLogFile(object method, Exception exception)
        {
            string startupPath = System.IO.Directory.GetCurrentDirectory();
            string projectPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            string folderName = Path.Combine(projectPath, "ErrorLog");
            System.IO.Directory.CreateDirectory(folderName);
            try
            {
                //Opens a new file stream which allows asynchronous reading and writing
                using (StreamWriter sw = new StreamWriter(new FileStream(folderName + "\\ErrorLog.txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    //Writes the method name with the exception and writes the exception underneath
                    sw.WriteLine(String.Format("{0} ({1}) - Method: {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), method.ToString()));
                    sw.WriteLine(exception.ToString()); sw.WriteLine("");
                }
            }
            catch (IOException)
            {
                if (!File.Exists(folderName))
                {
                    File.Create(folderName);
                }
            }
        }
    }
}
