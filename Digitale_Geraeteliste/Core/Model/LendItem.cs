using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Model
{
    internal class LendItem
    {
        public int Id { get; set; }
        public DateTime LendDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public DateTime ActualReturnDate { get; set; } = DateTime.Empty;
        public Employee BorrowedBy { get; set; }
        public Item Item { get; set; }
        public Employee LendBy { get; set; }

        public LendItem(int id, DateTime lendDate, DateTime expectedReturnDate, Employee borrowedBy, Item item, Employee lendBy)
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
            ExpectedReturnDate = LendDate.AddDays(LendItem.StandardLendDuration);
        }

        public void LendItemToEmployee(Employee employee, DateTime dateTime, int duration)
        {
            BorrowedBy = employee;
            LendDate = dateTime;
            ExpectedReturnDate = LendDate.AddDays(duration);
        }
    }
}
