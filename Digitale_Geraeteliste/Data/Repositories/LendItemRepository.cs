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
        private readonly string _logPath = $"{Path.Combine(AppContext.BaseDirectory, "Logs", $"log{DateTime.Now:yyyyMMdd}.txt")}";
        public LendItemRepository(LendContext context)
        {
            _context = context;
        }
        public void ChangeLendItem(int lendItemId, int itemId, int borrowedById, int lendById, DateTime lendDate, int duration, string affiliatedContractNumber)
        {
            LendItem lendItem = _context.LendItems.Find(lendItemId) ?? throw new ArgumentException("Lend item not found", nameof(lendItemId));
            if (lendItem.IsActive == false)
            {
                throw new InvalidOperationException("Cannot change a lend item that is returned.");
            }
            Item item = _context.Items.Find(itemId) ?? throw new ArgumentException("Item not found", nameof(itemId));
            Employee borrowedBy = _context.Employees.Find(borrowedById) ?? throw new ArgumentException("Employee not found", nameof(borrowedById));
            Employee lendBy = _context.Employees.Find(lendById) ?? throw new ArgumentException("Employee not found", nameof(lendById));
            List<string> toLog = new List<string>
            {
                $"----------------------------------------------------------------",
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] - Lend item with ID {lendItemId} changed.",
                $"Old values:",
                $"\t\t\t Item ID: {lendItem.ItemId}, Borrowed By: {lendItem.BorrowedBy}, Lend By: {lendItem.LendBy}",
                $"\t\t\t Lend Date: {lendItem.LendDate}, Expected Return Date: {lendItem.ExpectedReturnDate}",
                $"\t\t\t Affiliated Contract Number: {lendItem.AffiliatedContractNumber}, Duration: {lendItem.ExpectedReturnDate.Subtract(lendItem.LendDate).Days} days",
            };
            lendItem.Item = item;
            lendItem.BorrowedBy = borrowedBy;
            lendItem.BorrowedById = borrowedById;
            lendItem.LendBy = lendBy;
            lendItem.LendById = lendById;
            lendItem.LendDate = lendDate;
            if (duration > 0)
            {
                lendItem.ExpectedReturnDate = lendDate.AddDays(duration);
            }
            else
            {
                lendItem.ExpectedReturnDate = lendDate.AddDays(item.StandardLendDuration);
            }
            lendItem.AffiliatedContractNumber = affiliatedContractNumber;
            string[] newValues = new string[]
            {   $"\t\t\t |||||||||||||||||||||||||||||",
                $"\t\t\t vvvvvvvvvvvvvvvvvvvvvvvvvvvvv",
                $"New values:",
                $"\t\t\t Item ID: {lendItem.ItemId}, Borrowed By: {lendItem.BorrowedBy}, Lend By: {lendItem.LendBy}",
                $"\t\t\t Lend Date: {lendItem.LendDate}, Expected Return Date: {lendItem.ExpectedReturnDate}",
                $"\t\t\t Affiliated Contract Number: {lendItem.AffiliatedContractNumber}, Duration: {lendItem.ExpectedReturnDate.Subtract(lendItem.LendDate).Days} days",
            };
            toLog.AddRange(newValues);
            IEnumerable<string> logStrings = toLog;
            File.AppendAllLines(_logPath, logStrings);
            _context.SaveChanges();
        }

        public bool CreateNewLendItem(int itemId, int borrowedById, int lendById, DateTime lendDate, string affiliatedContractNumber, int duration)
        {
            Item item = _context.Items.Find(itemId) ?? throw new ArgumentException("Item not found", nameof(itemId));
            Employee lendBy = _context.Employees.Find(lendById) ?? throw new ArgumentException("Employee not found", nameof(lendById));
            Employee borrowedBy = _context.Employees.Find(borrowedById) ?? throw new ArgumentException("Employee not found", nameof(borrowedById));
            LendItem lendItem = new LendItem(lendDate, borrowedBy, item, lendBy, affiliatedContractNumber, duration);
            _context.LendItems.Add(lendItem);
            _context.SaveChanges();
            return true;
        }

        public IEnumerable<LendItem> GetAllLendItems()
        {
            return _context.LendItems;
        }

        public IEnumerable<LendItem> GetAllOverdueLendItems()
        {
            List<LendItem> overdueLendItems = new List<LendItem>();
            foreach (var lendItem in _context.LendItems)
            {
                if (lendItem.IsOverdue)
                {
                    overdueLendItems.Add(lendItem);
                }
            }
            return overdueLendItems;
        }

        public LendItem GetLendItemById(int id)
        {
            return _context.LendItems.Find(id) ?? throw new ArgumentException("Lend item not found", nameof(id));
        }

        public void ReturnLendItem(int lendItemId, DateTime returnDate)
        {
            LendItem? lendItem = _context.LendItems.Find(lendItemId);
            if (lendItem != null)
            {
                lendItem.ReturnItem(returnDate);
                _context.SaveChanges();
            }
        }

        public bool ImportAllCSVData()
        {
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Logs")))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Logs"));
            }
            if (_context.Categories.Any() || _context.Items.Any() || _context.Employees.Any() || _context.LendItems.Any())
            {
                string logstring = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Database already contains data. Skipping CSV import.";
                File.AppendAllLines(_logPath, new[] { logstring });
                return false;
            }
            var employeePath = Path.Combine(AppContext.BaseDirectory, "Data", "Testdata", "mitarbeiter.csv");
            var categoryPath = Path.Combine(AppContext.BaseDirectory, "Data", "Testdata", "kategorien.csv");
            var itemPath = Path.Combine(AppContext.BaseDirectory, "Data", "Testdata", "geraete.csv");
            var lendItemPath = Path.Combine(AppContext.BaseDirectory, "Data", "Testdata", "ausleihen.csv");
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
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                string logstring = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Error importing CSV data: {ex.Message}";
                File.AppendAllLines(_logPath, new[] { logstring });
                return false;
            }
        }
    }
}
