using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Model
{
    public class Employee
    {
        public int Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;

        public Employee(int id, string lastName, string firstName, string department)
        {
            Id = id;
            LastName = lastName;
            FirstName = firstName;
            Department = department;
            FullName = CreateFullName(firstName, lastName);
        }

        private Employee() { }
        public string CreateFullName(string firstName, string lastName)
        {
            string fullName = firstName + " " + lastName;
            return fullName;
        }
    }
}
