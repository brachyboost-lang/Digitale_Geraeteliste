using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Services;
using Digitale_Geraeteliste.Core.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

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

        public MainViewModel(ILendService lendService)
        {
            _lendService = lendService;
            Refresh();
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