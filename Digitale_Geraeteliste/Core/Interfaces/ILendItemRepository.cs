using System;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Text;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface ILendItemRepository
    {
        LendItem CreateNewLendItem(int itemId, int employeeId, DateTime lendDate, DateTime returnDate);
        LendItem GetLendItemById(int id);
        IEnumerable<LendItem> GetAllLendItems();
        void ChangeLendItem(LendItem lendItem, int itemId, int employeeId, DateTime lendDate, DateTime returnDate);
        void ReturnLendItem(int lendItemId, DateTime returnDate);
        IEnumerable<LendItem> GetAllOverdueLendItems();
    }
}
