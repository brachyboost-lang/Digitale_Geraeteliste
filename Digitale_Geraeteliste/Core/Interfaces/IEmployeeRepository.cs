using System;
using System.Collections.Generic;
using System.Text;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface IEmployeeRepository
    {
        bool CreateNewEmployee(string firstName, string lastName, string department);
        bool ChangeEmployee(Employee employee, string firstName, string lastName, string department);
        Employee GetEmployeeByID(int id);
    }
}
