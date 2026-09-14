using System;
using System.Collections.Generic;
using System.Text;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface IEmployeeRepository
    {
        public abstract Employee CreateNewEmployee(string firstName, string lastName, string email, string phoneNumber, string department);
        public abstract Employee ChangeEmployee(Employee employee, string firstName, string lastName, string email, string phoneNumber, string department);
        public abstract Employee SaveEmployee(Employee employee);
        public abstract Employee GetEmployeeByID(int id);
    }
}
