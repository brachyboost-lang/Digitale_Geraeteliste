using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Model
{
    internal class Employee
    {
        public int Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}
