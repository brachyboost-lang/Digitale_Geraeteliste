using Digitale_Geraeteliste.Data.Repositories;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Digitale_Geraeteliste
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        LendContext lendContext = new LendContext();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            EmployeeRepository employeeRepository = new EmployeeRepository(lendContext);
            ItemRepository itemRepository = new ItemRepository(lendContext);
            LendItemRepository lendItemRepository = new LendItemRepository(lendContext);
            lendItemRepository.ImportAllCSVData();
        }
    }

}
