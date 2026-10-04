using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Services;
using Digitale_Geraeteliste.Core.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace Digitale_Geraeteliste.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ILendService _lendService;
        private InventoryOverviewRow? _selectedRow;

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
            Refresh();
        }
        LendCommand = new RelayCommand(_ => OpenLendDialog(), _ => SelectedRow?.Status == ItemStatus.Available);

        private void OpenLendDialog()
        {
            var dialogViewModel = new LendDialogViewModel(_lendService, SelectedRow!, _lendService.GetEmployees());
            var dialog = new LendDialog(dialogViewModel) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                Refresh();
            }
        }
        private void ReturnSelected()
        {
            MessageBox.Show("Möchten Sie den ausgewählten Artikel zurückgeben?", "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
        public void Refresh()
        {
            Rows.Clear();
            foreach (InventoryOverviewRow row in _lendService.GetInventoryOverview(DateTime.Today))
            {
                Rows.Add(row);
            }
        }
    }
}