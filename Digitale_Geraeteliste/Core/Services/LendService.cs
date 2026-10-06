using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using Microsoft.VisualBasic;

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
        public IEnumerable<Employee> GetStorageEmployees() => _employeeRepository.GetEmployeesByDepartment("Lager");
        // Spezifisch für die alten Stammdaten, eventuell umbau nötig in zukunft - might need to be refactored in the future

        public IEnumerable<Employee> GetAllEmployees() => _employeeRepository.GetAllEmployees();
        public IEnumerable<InventoryOverviewRow> GetInventoryOverview(DateTime dateToCheck)
        {
            Dictionary<int, LendItem> openLends = _lendItemRepository.GetAllLendItems()
                   .Where(l => l.ActualReturnDate == null)
                   .GroupBy(l => l.ItemId)
                   .ToDictionary(g => g.Key, g => g.First());
            List<InventoryOverviewRow> overviewRows = new List<InventoryOverviewRow>();
            foreach (var item in _itemRepository.GetAllItems())
            {
                openLends.TryGetValue(item.Id, out LendItem? lendItem);
                overviewRows.Add(new InventoryOverviewRow
                {
                    ItemId = item.Id,
                    LendItemID = lendItem?.Id,
                    InventoryNumber = item.InventoryNumber,
                    Name = item.Name,
                    LendByEmployeeName = lendItem?.LendBy.FullName,
                    BorrowedByEmployeeName = lendItem?.BorrowedBy.FullName,
                    ExpectedReturnDate = lendItem?.ExpectedReturnDate,
                    Status = DetermineItemStatus(item, lendItem, dateToCheck),
                    CategoryName = item.Category.Name,
                });
            }
            return overviewRows;
        }
        private static ItemStatus DetermineItemStatus(Item item, LendItem? openLend, DateTime dateToCheck)
        {
            if (item.IsRetired)
            {
                return ItemStatus.Retired;
            }
            if (openLend == null)
            {
                return ItemStatus.Available;
            }
            return openLend.IsOverdueAt(dateToCheck) ? ItemStatus.Overdue : ItemStatus.LentOut;
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
            bool success = _lendItemRepository.CreateNewLendItem(itemId, borrowedById, lendById, lendDate, affiliatedContractNumber, expectedReturnDate);
            if (!success)
            {
                return TransactionResult.Failure("Failed to create new lend item.");
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
            if (returnDate.Date < item.LendDate.Date)
            {
                return TransactionResult.Failure("Return date cannot be before the lend date.");
            }
            if (item.ActualReturnDate.HasValue)
            {
                return TransactionResult.Failure("Item has already been returned.");
            }
            bool success = _lendItemRepository.ReturnLendItem(lendItemId, returnDate);
            if (!success)
            {
                return TransactionResult.Failure("Failed to return lend item.");
            }
            return TransactionResult.Success();
        }
    }
}
