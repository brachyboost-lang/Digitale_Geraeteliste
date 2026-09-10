using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Model
{
    internal class LendItem
    {
        internal int Id { get; set; }
        internal DateTime LendDate { get; set; }
        internal DateTime ExpectedReturnDate { get; set; }
        internal DateTime? ActualReturnDate { get; set; } ;
        internal Employee BorrowedBy { get; set; }
        internal Item Item { get; set; }
        internal Employee LendBy { get; set; }
        internal bool IsActive { get; set; } = true;
        internal bool IsOverdue => !IsReturned && DateTime.Now > ExpectedReturnDate;
        internal string AffiliatedContractNumber { get; set; } = string.Empty;
        
        internal LendItem(int id, DateTime lendDate, DateTime expectedReturnDate, Employee borrowedBy, Item item, Employee lendBy)
        {
            Id = id;
            LendDate = lendDate;
            ExpectedReturnDate = expectedReturnDate;
            BorrowedBy = borrowedBy;
            Item = item;
            LendBy = lendBy;
        }

        public void LendItemToEmployee(Employee employee, DateTime dateTime)
        {
            BorrowedBy = employee;
            LendDate = dateTime;
            ExpectedReturnDate = LendDate.AddDays(LendItem.Item.StandardLendDuration);
        }

        public void LendItemToEmployee(Employee employee, DateTime dateTime, int duration)
        {
            BorrowedBy = employee;
            LendDate = dateTime;
            ExpectedReturnDate = LendDate.AddDays(duration);
        }
    }
}
