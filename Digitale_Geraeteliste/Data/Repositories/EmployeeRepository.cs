using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Data.Repositories
{
    internal class EmployeeRepository : IEmployeeRepository
    {
        private readonly LendContext _context;
        public EmployeeRepository(LendContext context)
        {
            _context = context;
        }
        public bool ChangeEmployee(Employee employee, string firstName, string lastName, string department)
        {
            int changes = 0;
            Employee employeeToChange = _context.Employees.Find(employee.Id) ?? throw new ArgumentException("Employee not found", nameof(employee));
            employeeToChange.FirstName = firstName;
            employeeToChange.LastName = lastName;
            employeeToChange.FullName = employeeToChange.CreateFullName(firstName, lastName);
            employeeToChange.Department = department;
            changes = _context.SaveChanges();
            return changes > 0;
        }

        public bool CreateNewEmployee(string firstName, string lastName, string department)
        {
            var employee = new Employee(lastName, firstName, department);
            _context.Employees.Add(employee);
            _context.SaveChanges();
            return true;
        }

        public Employee GetEmployeeByID(int id)
        {
            return _context.Employees.Find(id) ?? throw new ArgumentException("Employee not found", id.ToString());
        }
    }
}
