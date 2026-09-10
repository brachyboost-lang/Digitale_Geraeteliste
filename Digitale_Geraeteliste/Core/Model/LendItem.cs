using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Model
{
    internal class LendItem
    {
        internal int Id { get; set; }
        internal DateTime LendDate { get; set; }
        internal DateOnly ExpectedReturnDate { get; set; }
        internal DateTime? ActualReturnDate { get; set; }
        internal Employee BorrowedBy { get; set; }
        internal Item Item { get; set; }
        internal Employee LendBy { get; set; }
        internal bool IsActive { get; set; } = true;
        internal bool IsOverdue => IsOverdueAt(DateOnly.FromDateTime(DateTime.Now));
        internal string AffiliatedContractNumber { get; set; } = string.Empty;
        
        internal LendItem(int id, DateTime lendDate, DateOnly expectedReturnDate, Employee borrowedBy, Item item, Employee lendBy)
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
            ExpectedReturnDate = (DateOnly.FromDateTime(LendDate).AddDays(Item.StandardLendDuration));
        }

        public void LendItemToEmployee(Employee employee, DateTime dateTime, int duration)
        {
            BorrowedBy = employee;
            LendDate = dateTime;
            ExpectedReturnDate = (DateOnly.FromDateTime(LendDate).AddDays(duration));
        }
        internal bool IsOverdueAt(DateOnly dayToCheck) => ActualReturnDate == null && dayToCheck > ExpectedReturnDate;
    }
}
