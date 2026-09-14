using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Data.Repositories
{
    internal class EmployeeRepository : IEmployeeRepository
    {
        public Employee ChangeEmployee(Employee employee, string firstName, string lastName, string email, string phoneNumber, string department)
        {
            throw new NotImplementedException();
        }

        public Employee CreateNewEmployee(string firstName, string lastName, string email, string phoneNumber, string department)
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
