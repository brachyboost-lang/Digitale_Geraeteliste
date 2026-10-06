using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using Digitale_Geraeteliste.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Digitale_Geraeteliste.Views;

namespace Digitale_Geraeteliste.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ILendService _lendService;
        private InventoryOverviewRow? _selectedRow;
        public RelayCommand LendCommand { get; }

        public ObservableCollection<InventoryOverviewRow> Rows { get; } = new ObservableCollection<InventoryOverviewRow>();

        public InventoryOverviewRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                _selectedRow = value;
                OnPropertyChanged();
            }
        }
        public RelayCommand ReturnCommand { get; }

        public MainViewModel(ILendService lendService)
        {
            _lendService = lendService;
            ReturnCommand = new RelayCommand(_ => ReturnSelected(), _ => SelectedRow?.LendItemID != null);
            LendCommand = new RelayCommand(_ => OpenLendDialog(), _ => SelectedRow?.Status == ItemStatus.Available);
            Refresh();
        }

        private void ReturnSelected()
        {
            if (MessageBoxResult.Yes == MessageBox.Show("Möchten Sie den ausgewählten Artikel zurückgeben?", "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                var result = _lendService.ReturnItem(SelectedRow!.LendItemID!.Value, DateTime.Today);
                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    Refresh();
                }
            }
        }
        private ItemStatus? _selectedStatus;
        public ItemStatus? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged();
                Refresh();
            }
        }
        private string _textBoxFilter = string.Empty;
        public string TextBoxFilter
        {
            get => _textBoxFilter;
            set
            {
                _textBoxFilter = value;
                OnPropertyChanged();
                Refresh();
            }
        }
        public void Refresh()
        {
            Rows.Clear();
            if (TextBoxFilter != null && TextBoxFilter.Length > 0 && TextBoxFilter.Trim().Length > 0)
            {
                foreach (InventoryOverviewRow row in _lendService.GetInventoryOverview(DateTime.Today))
                {
                    if (row.InventoryNumber.Contains(TextBoxFilter, StringComparison.OrdinalIgnoreCase) || row.Name.Contains(TextBoxFilter, StringComparison.OrdinalIgnoreCase) || 
                        (row.Status.ToString().Contains(TextBoxFilter, StringComparison.OrdinalIgnoreCase)) || (row.BorrowedByEmployeeName != null && row.BorrowedByEmployeeName.Contains(TextBoxFilter, StringComparison.OrdinalIgnoreCase))
                        || (row.LendByEmployeeName != null && row.LendByEmployeeName.Contains(TextBoxFilter, StringComparison.OrdinalIgnoreCase)) || (row.CategoryName != null && row.CategoryName.Contains(TextBoxFilter, StringComparison.OrdinalIgnoreCase))
                            || (row.ExpectedReturnDate != null && row.ExpectedReturnDate.Value.ToString("dd.MM.yyyy").Contains(TextBoxFilter, StringComparison.OrdinalIgnoreCase))
                            || (row.LendDate != null && row.LendDate.Value.ToString("dd.MM.yyyy").Contains(TextBoxFilter, StringComparison.OrdinalIgnoreCase)))
                    {
                        Rows.Add(row);
                    }
                }
                return;
            }
            else
            {
                switch (SelectedStatus)
                {
                    case ItemStatus.Available:
                        foreach (InventoryOverviewRow row in _lendService.GetInventoryOverview(DateTime.Today))
                        {
                            if (row.Status == ItemStatus.Available)
                            {
                                Rows.Add(row);
                            }
                        }
                        break;
                    case ItemStatus.LentOut:
                        foreach (InventoryOverviewRow row in _lendService.GetInventoryOverview(DateTime.Today))
                        {
                            if (row.Status == ItemStatus.LentOut || row.Status == ItemStatus.Overdue)
                            {
                                Rows.Add(row);
                            }
                        }
                        break;
                    case ItemStatus.Overdue:
                        foreach (InventoryOverviewRow row in _lendService.GetInventoryOverview(DateTime.Today))
                        {
                            if (row.Status == ItemStatus.Overdue)
                            {
                                Rows.Add(row);
                            }
                        }
                        break;
                    default:
                        foreach (InventoryOverviewRow row in _lendService.GetInventoryOverview(DateTime.Today))
                        {
                            Rows.Add(row);
                        }
                        break;
                }
            }
        }
        private void OpenLendDialog()
        {
            var dialogViewModel = new NewLendDialogViewModel(_lendService, SelectedRow!, _lendService.GetStorageEmployees(), _lendService.GetAllEmployees());
            var dialog = new NewLendDialog(dialogViewModel) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                Refresh();
            }
        }
        public ICommand ShowOpenLendsCommand => new RelayCommand(_ => ShowOpenLends());
        public ICommand ShowOverdueLendsCommand => new RelayCommand(_ => ShowOverdueLends());
        public ICommand ShowAllItemsCommand => new RelayCommand(_ => ShowAllItems());

        private void ShowOpenLends()
        {
            SelectedStatus = ItemStatus.LentOut;
        }
        private void ShowOverdueLends()
        {
            SelectedStatus = ItemStatus.Overdue;
        }
        private void ShowAllItems()
        {
            SelectedStatus = null;
            TextBoxFilter = string.Empty;
        }
    }
}