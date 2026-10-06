using Digitale_Geraeteliste.Core.Model;
using Digitale_Geraeteliste.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface ILendService
    {
        IEnumerable<Employee> GetStorageEmployees();
        IEnumerable<Employee> GetAllEmployees();
        TransactionResult BorrowItem(int itemId, int borrowedById, int lendById, DateTime lendDate, DateTime? expectedReturnDate, string affiliatedContractNumber);
        TransactionResult ReturnItem(int lendItemId, DateTime returnDate);
        IEnumerable<InventoryOverviewRow> GetInventoryOverview(DateTime dateToCheck);
    }
}
