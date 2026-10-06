using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using Digitale_Geraeteliste.Core.Services;
using Digitale_Geraeteliste.Data.Repositories;
using Digitale_Geraeteliste.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

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
        public ICommand ExportOverdueLendsCommand => new RelayCommand(_ => ExportOverdueLends());

        private void ExportOverdueLends()
        {
            DateTime today = DateTime.Today;
            List<InventoryOverviewRow> overdue = _lendService.GetInventoryOverview(today).Where(r => r.Status == ItemStatus.Overdue).ToList();
            if (overdue.Count == 0)
            {
                MessageBox.Show("Es gibt aktuell keine überfälligen Verleihe.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Ueberfaellig_{today:yyyy-MM-dd}.csv",
                Filter = "CSV-Datei (*.csv)|*.csv"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            try
            {
                int count = CSVExport.WriteOverdueList(overdue, dialog.FileName, today);
                MessageBox.Show($"{count} überfällige Verleihe exportiert.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (IOException)
            {
                MessageBox.Show("Die Datei konnte nicht geschrieben werden. Ist sie noch in Excel geöffnet?", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}