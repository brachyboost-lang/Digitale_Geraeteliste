using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.RightsManagement;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Data.Repositories
{
    public class CSVImporter
    {

        public static IEnumerable<Employee> GetEmployeesFromCSV(string filePath)
        {
            var employees = new List<Employee>();
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine(); // skips the header line - ghetto fix for my testdata csv, could be improved by checking if the first line is a header
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        var values = line.Split(';');
                        var employee = new Employee(
                            firstName: values[2],
                            lastName: values[1],
                            department: values[4]
                        );
                        employee.Id = int.Parse(values[0]);
                        employees.Add(employee);
                    }
                }
            }
            return employees;
        }
        public static IEnumerable<Category> GetCategoriesFromCSV(string filePath)
        {
            var categories = new List<Category>();
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine(); // skips the header line - ghetto fix for my testdata csv, could be improved by checking if the first line is a header
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        var values = line.Split(';');
                        var category = new Category(
                            name: values[1]
                        );
                        category.Id = int.Parse(values[0]);
                        categories.Add(category);
                    }
                }
            }
            return categories;
        }
        public static IEnumerable<Item> GetItemsFromCSV(string filePath, IEnumerable<Category> categories)
        {
            var items = new List<Item>();
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine(); // skips the header line - ghetto fix for my testdata csv, could be improved by checking if the first line is a header
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        var values = line.Split(';');
                        var categoryId = int.Parse(values[3]);
                        var category = categories.FirstOrDefault(c => c.Id == categoryId) ?? throw new InvalidOperationException($"Category with ID {categoryId} not found");
                        var item = new Item(
                            inventoryNumber: values[1],
                            name: values[2],
                            category: category,
                            description: values[4],
                            standardLendDuration: int.Parse(values[5]),
                            isRetired: bool.Parse(values[7]),
                            needsMaintenance: bool.Parse(values[8])
                        );
                        item.Id = int.Parse(values[0]);
                        items.Add(item);
                    }
                }
            }
            return items;
        }
        public static IEnumerable<LendItem> GetLendItemsFromCSV(string filePath, IEnumerable<Item> items, IEnumerable<Employee> employees)
        {
            var lendItems = new List<LendItem>();
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine(); // skips the header line - ghetto fix for my testdata csv, could be improved by checking if the first line is a header
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        var values = line.Split(';');
                        var id = int.Parse(values[0]);
                        var itemId = int.Parse(values[1]);
                        var borrowedById = int.Parse(values[2]);
                        var item = items.FirstOrDefault(i => i.Id == itemId) ?? throw new InvalidOperationException($"Item with ID {itemId} not found");
                        var borrowedBy = employees.FirstOrDefault(e => e.Id == borrowedById) ?? throw new InvalidOperationException($"Employee with ID {borrowedById} not found");
                        var lendById = int.Parse(values[3]);
                        var lendBy = employees.FirstOrDefault(e => e.Id == lendById) ?? throw new InvalidOperationException($"Employee with ID {lendById} not found");
                        var lendDate = DateTime.Parse(values[4]);
                        var affiliatedContractNumber = values[8];
                        var lendItem = new LendItem(
                            lendDate : lendDate,
                            borrowedBy: borrowedBy,
                            item: item,
                            lendBy: lendBy,
                            affiliatedContractNumber: affiliatedContractNumber,
                            actualReturnDate: string.IsNullOrEmpty(values[6]) ? null : DateTime.Parse(values[6]),
                            isActive: bool.Parse(values[7]),
                            expectedReturnDate: DateTime.Parse(values[5])
                        );
                        lendItem.Id = int.Parse(values[0]);
                        lendItems.Add(lendItem);
                    }
                }
            }
            return lendItems;
        }
    }
}
