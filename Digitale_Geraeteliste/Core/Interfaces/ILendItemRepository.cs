using System;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Text;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface ILendItemRepository
    {
        bool CreateNewLendItem(int itemId, int borrowedById, int lendById, DateTime lendDate, string affiliatedContractNumber, DateTime? expectedReturnDate);
        LendItem? GetLendItemById(int id);
        IEnumerable<LendItem> GetAllLendItems();
        bool ChangeLendItem(int lendItemId, int itemId, int borrowedById, int lendById, DateTime lendDate, DateTime? expectedReturnDate, string affiliatedContractNumber);
        bool ReturnLendItem(int lendItemId, DateTime returnDate);
        IEnumerable<LendItem> GetAllOverdueLendItems();
        LendItem? GetOpenLendByItemId(int itemId);
    }
}
