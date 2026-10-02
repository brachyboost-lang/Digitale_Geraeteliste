using System;
using System.Collections.Generic;
using System.Text;
using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Data.Repositories;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Core.Services
{
    public class LendService : ILendService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILendItemRepository _lendItemRepository;
        public LendService(IItemRepository itemRepository, ILendItemRepository lendItemRepository)
        {
            _itemRepository = itemRepository;
            _lendItemRepository = lendItemRepository;
        }
        public TransactionResult BorrowItem(int itemId, int borrowedById, int lendById, DateTime lendDate, int duration, string affiliatedContractNumber)
        {
            var item = _itemRepository.GetItemById(itemId);
            if (item == null)
            {
                return TransactionResult.Failure("Item not found.");
            }
            if (duration < 0)
            {
                return TransactionResult.Failure("Invalid duration.");
            }
            if (item.IsRetired)
            {
                return TransactionResult.Failure("Item is retired and cannot be borrowed.");
            }
            LendItem? existingLendItem = _lendItemRepository.GetOpenLendByItemId(itemId);
            if (existingLendItem != null)
            {
                return TransactionResult.Failure("Item is already borrowed.");
            }


            return TransactionResult.Success();
        }
        public TransactionResult ReturnItem(int lendItemId, DateTime returnDate)
        {
            var item = _lendItemRepository.GetLendItemById(lendItemId);
            if (item == null)
            {
                return TransactionResult.Failure("Item not found.");
            }
            return TransactionResult.Success();
        }
    }
}
