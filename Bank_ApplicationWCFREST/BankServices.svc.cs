using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Bank_ApplicationWCFREST
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IBank
    {
        BankDataModelDataContext data = new BankDataModelDataContext();

        public bool AddAccount(Account account)
        {
            try
            {
                string salt = Guid.NewGuid().ToString().Substring(0, 7);
                account.salt = salt;
                string pin = "123456";
                var str = pin + salt;
                var MD5Pass = Encryptor.MD5Hash(str);
                account.pin = MD5Pass;
                account.balance = 50000;
                account.createAt = DateTime.Now;
                account.updateAt = DateTime.Now;
                account.status = 0;
                data.Accounts.InsertOnSubmit(account);
                data.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddCustomer(Customer customer)
        {
            try
            {
                if (data.Customers.Any(x => x.accountId == customer.accountId))
                {
                    return false;
                }

                var accountID = customer.accountId;


                Account accountModify = (from account in data.Accounts
                                         where account.accountNumber == accountID
                                         && account.status == 0
                                         select account).Single();
                if (accountModify == null)
                {
                    return false;
                }

                customer.createAt = DateTime.Now;
                customer.updateAt = DateTime.Now;
                accountModify.status = 1;

                data.Customers.InsertOnSubmit(customer);
                data.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddPartnerAccount(PartnerAccount account)
        {
            try
            {
                if (data.PartnerAccounts.Any(x => x.accountNumber == account.accountNumber))
                {
                    return false;
                }

                var accountID = account.accountNumber;

                Account accountModify = (from c in data.Accounts
                                         where c.accountNumber == accountID
                                         && c.status == 1
                                         select c).Single();
                if (accountModify == null)
                {
                    return false;
                }

                string salt = Guid.NewGuid().ToString().Substring(0, 7);
                account.salt = salt;
                string pin = "123456";
                var str = pin + salt;
                var MD5Pass = Encryptor.MD5Hash(str);
                account.password = MD5Pass;
                account.status = 1;

                data.PartnerAccounts.InsertOnSubmit(account);
                data.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddHistoryTransaction(HistoryTransaction historyTransaction)
        {
            try
            {
                data.HistoryTransactions.InsertOnSubmit(historyTransaction);
                data.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int AddTransaction(Transaction transaction)
        {
            try
            {
                var paypalid = 300000000005;
                // Lấy ra thông tin tài khoản người gửi
                Account senderAccount = (from account in data.Accounts
                                         where account.accountNumber == transaction.senderAccountNumber
                                         && account.status == 1
                                         select account).Single();
                // Lấy ra thông tin tài khoản người nhận.
                Account receiverAccount = (from account in data.Accounts
                                           where account.accountNumber == transaction.receiverAccountNumber
                                           && account.status == 1
                                           select account).Single();
                // Lấy ra thông tin tài khoản thực hiện giao dịch.
                Account paypal = (from account in data.Accounts
                                           where account.accountNumber == paypalid
                                           && account.status == 1
                                           select account).Single();

                // Kiểm tra tài khoản người gửi.
                var queryBalance = (from b in data.Accounts
                                    where b.accountNumber == transaction.senderAccountNumber
                                    && b.status == 1
                                    select b.balance).FirstOrDefault().ToString();
                if (queryBalance == null)
                {
                    return -4;
                }

                // Lấy số dư tài khoản người nhận.
                var queryBalanceReceiver = (from b in data.Accounts
                                            where b.accountNumber == transaction.receiverAccountNumber
                                            && b.status == 1
                                            select b.balance).FirstOrDefault().ToString();

                if (queryBalanceReceiver == null)
                {
                    return -3;
                }

                // Kiểm tra tài khoản thực hiện giao dịch.
                var queryBalancePaypal = (from b in data.Accounts
                                            where b.accountNumber == paypalid
                                            && b.status == 1
                                            select b.balance).FirstOrDefault().ToString();

                if (queryBalancePaypal == null)
                {
                    return -2;
                }

                // Tính phí giao dịch
                decimal fee = 0;
                var amount = transaction.amount;
                if (amount <= 100000)
                {
                    fee = 10000;
                }
                else if (100000 < amount && amount <= 500000)
                {
                    decimal v = (2 * amount) / 100;
                    fee = v;
                }
                else if (500000 < amount && amount <= 1000000)
                {
                    decimal v = ((decimal)1.5 * amount) / 100;
                    fee = v;
                }
                else if (1000000 < amount && amount <= 5000000)
                {
                    decimal v = (1 * amount) / 100;
                    fee = v;
                }
                else if (amount > 5000000)
                {
                    decimal v = ((decimal)0.5 * amount) / 100;
                    fee = v;
                }
               
                //Nếu số tiền nhập lớn hơn số tiền trong tài khoản.Báo lỗi.
                if ((amount + fee) > Convert.ToInt64(queryBalance))
                {
                    return -1;
                }
                
                // Trừ tiền tài khoản người gửi.
                senderAccount.balance = Convert.ToInt64(queryBalance) - amount - fee;
                senderAccount.updateAt = DateTime.Now;
                
                // Update số tiền vào tài khoản người nhận.
                receiverAccount.balance = Convert.ToInt64(queryBalanceReceiver) + amount;
                receiverAccount.updateAt = DateTime.Now;

                // Update phí giao dịch vào tài khoản thực hiện giao dịch.
                paypal.balance = Convert.ToInt64(queryBalancePaypal) + fee;
                paypal.updateAt = DateTime.Now;
                
                transaction.feeTransaction = fee;
                data.Transactions.InsertOnSubmit(transaction);
                data.SubmitChanges();
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        public List<Transaction> GetTransactionList(string id)
        {
            try
            {
                return (from historyTransaction in data.Transactions where historyTransaction.senderAccountNumber == Convert.ToInt64(id)
                        || historyTransaction.receiverAccountNumber == Convert.ToInt64(id)
                        select historyTransaction).ToList();
            }
            catch
            {
                return null;
            }
        }

        public bool LoginAccount(Account customer)
        {
            try
            {
                string pass = customer.pin.ToString();

                var acountSalt = (from c in data.Accounts
                                  where c.accountNumber == customer.accountNumber
                                  select c.salt).FirstOrDefault().ToString();

                if (acountSalt == null)
                {
                    return false;
                }
                var str = pass + acountSalt;
                var MD5Pass = Encryptor.MD5Hash(str);

                var acount = data.Accounts.Where(x => x.accountNumber == customer.accountNumber && x.pin == MD5Pass).FirstOrDefault();

                if (acount == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool LoginPartnerAccount(PartnerAccount partner)
        {
            try
            {
                string pass = partner.password.ToString();

                var acountSalt = (from c in data.PartnerAccounts
                                  where c.partnerAccount1 == partner.partnerAccount1
                                  select c.salt).FirstOrDefault().ToString();

                if (acountSalt == null)
                {
                    return false;
                }
                var str = pass + acountSalt;
                var MD5Pass = Encryptor.MD5Hash(str);

                var acount = data.PartnerAccounts.Where(x => x.partnerAccount1 == partner.partnerAccount1 && x.password == MD5Pass).FirstOrDefault();

                if (acount == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

       
        public PartnerAccount GetPartnerById(long id)
        {
            var account = data.PartnerAccounts.Where(p=> p.partnerAccount1 == id).FirstOrDefault();
            return account;
        }
    }
}
