using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Digitale_Geraeteliste.Data.Repositories
{
    internal class LendItemRepository : ILendItemRepository
    {
        private readonly LendContext _context;
        public LendItemRepository(LendContext context)
        {
            _context = context;
        }
        public void ChangeLendItem(LendItem lendItem, int itemId, int employeeId, DateTime lendDate, DateTime returnDate)
        {
            throw new NotImplementedException();
        }

        public LendItem CreateNewLendItem(int itemId, int employeeId, DateTime lendDate, DateTime returnDate)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<LendItem> GetAllLendItems()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<LendItem> GetAllOverdueLendItems()
        {
            throw new NotImplementedException();
        }

        public LendItem GetLendItemById(int id)
        {
            throw new NotImplementedException();
        }

        public void ReturnLendItem(int lendItemId, DateTime returnDate)
        {
            throw new NotImplementedException();
        }

        public bool ImportAllCSVData()
        {
            if (_context.Categories.Any() || _context.Items.Any() || _context.Employees.Any() || _context.LendItems.Any())
            {
                Console.WriteLine("Database already contains data. Skipping CSV import.");
                return false;
            }
            var employeePath = "path/to/employees.csv";
            var categoryPath = "path/to/categories.csv";
            var itemPath = "path/to/items.csv";
            var lendItemPath = @"G:\C# Projects\IHK Probeprojekt\Digitale_Geraeteliste\Data\Testdata\ausleihen.csv";
            try
            {
                var employees = CSVImporter.GetEmployeesFromCSV(employeePath);
                var categories = CSVImporter.GetCategoriesFromCSV(categoryPath);
                var items = CSVImporter.GetItemsFromCSV(itemPath, categories);
                var lendItems = CSVImporter.GetLendItemsFromCSV(lendItemPath, items, employees);

                foreach (var category in categories)
                {
                    _context.Categories.Add(category);
                }
                foreach (var item in items)
                {
                    _context.Items.Add(item);
                }
                foreach (var employee in employees)
                {
                    _context.Employees.Add(employee);
                }
                foreach (var lend in lendItems)
                {
                    _context.LendItems.Add(lend);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing CSV data: {ex.Message}");
                return false;
            }
        }
    }
}
