using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Model
{
    public class LendItem
    {
        public int Id { get; set; }
        public DateTime LendDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public Employee BorrowedBy { get; set; }
        public Item Item { get; set; }
        public Employee LendBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsOverdue => IsOverdueAt(DateTime.Now.Date);
        public string AffiliatedContractNumber { get; set; } = string.Empty;
        
        public LendItem(int id, DateTime lendDate, DateTime expectedReturnDate, Employee borrowedBy, Item item, Employee lendBy)
        {
            Id = id;
            LendDate = lendDate;
            ExpectedReturnDate = expectedReturnDate;
            BorrowedBy = borrowedBy;
            Item = item;
            LendBy = lendBy;
        }
        private LendItem() { }

        public void LendItemToEmployee(Employee employee, DateTime dateTime)
        {
            BorrowedBy = employee;
            LendDate = dateTime;
            ExpectedReturnDate = LendDate.AddDays(Item.StandardLendDuration);
        }

        public void LendItemToEmployee(Employee employee, DateTime dateTime, int duration)
        {
            BorrowedBy = employee;
            LendDate = dateTime;
            ExpectedReturnDate = LendDate.AddDays(duration);
        }
        internal bool IsOverdueAt(DateTime dayToCheck) => ActualReturnDate == null && dayToCheck > ExpectedReturnDate;
    }
}
