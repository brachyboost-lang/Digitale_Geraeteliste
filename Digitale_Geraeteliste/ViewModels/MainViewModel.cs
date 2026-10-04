using Digitale_Geraeteliste.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ILendService LendService { get; }
        public MainViewModel(ILendService _iLendService)
        {

            // Initialize properties and commands here
        }
    }
}
