using System;
using System.Collections.Generic;
using System.Text;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface IEmployeeRepository
    {
        Employee CreateNewEmployee(string firstName, string lastName, string email, string phoneNumber, string department);
        Employee ChangeEmployee(Employee employee, string firstName, string lastName, string email, string phoneNumber, string department);
        Employee SaveEmployee(Employee employee);
        Employee GetEmployeeByID(int id);
    }
}
