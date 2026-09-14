using System;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Text;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface ILendItemRepository
    {
        public abstract LendItem CreateNewLendItem(int itemId, int employeeId, DateTime lendDate, DateTime returnDate);
        public abstract LendItem GetLendItemById(int id);
        public abstract IEnumerable<LendItem> GetAllLendItems();
        public abstract void ChangeLendItem(LendItem lendItem, int itemId, int employeeId, DateTime lendDate, DateTime returnDate);
        public abstract void SaveLendItem(LendItem lendItem);
        public abstract void ReturnLendItem(int lendItemId, DateTime returnDate);

    }
}
