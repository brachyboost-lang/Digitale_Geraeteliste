using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Services
{
    public class EmployeeService
    {
        private readonly IEmployeeRepository _categories;
        public EmployeeService(IEmployeeRepository categories)
        {
            _categories = categories;
        }
    }
}
