using System;
using System.Collections.Generic;
using System.Text;
using Digitale_Geraeteliste.Core.Services;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface ILendService
    {
        TransactionResult BorrowItem(int itemId, int borrowedById, int lendById, DateTime lendDate, int duration, string affiliatedContractNumber);
        TransactionResult ReturnItem(int lendItemId, DateTime returnDate);
    }
}
