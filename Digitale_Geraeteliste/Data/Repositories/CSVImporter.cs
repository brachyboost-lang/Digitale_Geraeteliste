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
                            lastName: values[1],
                            firstName: values[2],
                            department: values[4]
                        );
                        employees.Add(employee);
                    }
                }
            }
            return employees;
        }
    }
}
