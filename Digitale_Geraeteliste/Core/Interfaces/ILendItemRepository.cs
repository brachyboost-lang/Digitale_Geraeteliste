using System;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Text;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface ILendItemRepository
    {
        bool CreateNewLendItem(int itemId, int borrowedById, int lendById, DateTime lendDate, string affiliatedContractNumber, int duration);
        LendItem GetLendItemById(int id);
        IEnumerable<LendItem> GetAllLendItems();
        void ChangeLendItem(int lendItemId, int itemId, int borrowedById, int lendById, DateTime lendDate, int duration, string affiliatedContractNumber);
        void ReturnLendItem(int lendItemId, DateTime returnDate);
        IEnumerable<LendItem> GetAllOverdueLendItems();
        IEnumerable<LendItem> GetOpenLendByItemId(int itemId);
    }
}
