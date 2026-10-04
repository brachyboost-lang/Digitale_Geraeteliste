using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using Digitale_Geraeteliste.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Digitale_Geraeteliste.ViewModels
{
    public class NewLendDialogViewModel : ViewModelBase
    {
        private readonly ILendService _lendService;
        private readonly int _itemId;

        public string ItemDisplay { get; }
        public IEnumerable<Employee> Employees { get; }
        public Employee? SelectedBorrower { get; set; }
        public Employee? SelectedLender { get; set; }
        public DateTime? LendDate { get; set; } = DateTime.Today;
        public DateTime? ExpectedReturnDate { get; set; }
        public string ContractNumber { get; set; } = string.Empty;

        public RelayCommand ConfirmCommand { get; }

        public event Action<bool>? CloseRequested;

        public NewLendDialogViewModel(ILendService lendService, InventoryOverviewRow row, IEnumerable<Employee> employees)
        {
            _lendService = lendService;
            _itemId = row.ItemId;
            ItemDisplay = $"{row.InventoryNumber} - {row.Name}";
            Employees = employees;
            ConfirmCommand = new RelayCommand(_ => Confirm(), _ => SelectedBorrower != null && SelectedLender != null && LendDate != null);
        }

        private void Confirm()
        {
            TransactionResult result = _lendService.BorrowItem(_itemId, SelectedBorrower!.Id, SelectedLender!.Id,
                                                               LendDate!.Value.Date, ExpectedReturnDate?.Date, ContractNumber);
            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage);
                return;
            }
            CloseRequested?.Invoke(true);
        }
    }
}

