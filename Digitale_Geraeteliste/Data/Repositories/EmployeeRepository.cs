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
            Employee employeeToChange = _context.Employees.Find(employee.Id) ?? throw new InvalidOperationException("Employee not found");
            if (employeeToChange != null)
            {
                employeeToChange.FirstName = firstName;
                employeeToChange.LastName = lastName;
                employeeToChange.CreateFullName(firstName, lastName);
                employeeToChange.Department = department;
                changes = _context.SaveChanges();
            }
            return changes > 0;
        }

        public Employee CreateNewEmployee(string firstName, string lastName, string department)
        {
            throw new NotImplementedException();
        }

        public Employee GetEmployeeByID(int id)
        {
            throw new NotImplementedException();
        }

        public Employee SaveEmployee(Employee employee)
        {
            throw new NotImplementedException();
        }
    }
}
