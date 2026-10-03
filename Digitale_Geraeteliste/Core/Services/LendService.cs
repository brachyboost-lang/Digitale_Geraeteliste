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
        private readonly IEmployeeRepository _employeeRepository;
        public LendService(IItemRepository itemRepository, ILendItemRepository lendItemRepository, IEmployeeRepository employeeRepository)
        {
            _itemRepository = itemRepository;
            _lendItemRepository = lendItemRepository;
            _employeeRepository = employeeRepository;
        }
        public TransactionResult BorrowItem(int itemId, int borrowedById, int lendById, DateTime lendDate, DateTime? expectedReturnDate, string affiliatedContractNumber)
        {
            var item = _itemRepository.GetItemById(itemId);
            if (item == null)
            {
                return TransactionResult.Failure("Item not found.");
            }
            if (expectedReturnDate.HasValue && expectedReturnDate.Value.Date < lendDate.Date)
            {
                return TransactionResult.Failure("Invalid return date.");
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
            _lendItemRepository.CreateNewLendItem(itemId, borrowedById, lendById, lendDate, affiliatedContractNumber, expectedReturnDate);
            return TransactionResult.Success();
        }
        public TransactionResult ReturnItem(int lendItemId, DateTime returnDate)
        {
            var item = _lendItemRepository.GetLendItemById(lendItemId);
            if (item == null)
            {
                return TransactionResult.Failure("Item not found.");
            }
            if (returnDate.Date < item.LendDate.Date)
            {
                return TransactionResult.Failure("Return date cannot be before the lend date.");
            }
            _lendItemRepository.ReturnLendItem(lendItemId, returnDate);
            return TransactionResult.Success();
        }
    }
}
