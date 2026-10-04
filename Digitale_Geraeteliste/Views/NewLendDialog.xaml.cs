using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Digitale_Geraeteliste.ViewModels;

namespace Digitale_Geraeteliste.Views
{
    public partial class NewLendDialog : Window
    {
        public NewLendDialog(NewLendDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseRequested += result => DialogResult = result;
        }
    }
}
