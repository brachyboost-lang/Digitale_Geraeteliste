using Digitale_Geraeteliste.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Services
{
    public class InventoryOverviewRow
    {
        public int ItemId { get; set; }
        public int? LendItemID { get; set; }
        public string InventoryNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? LendByEmployeeName { get; set; }
        public string? BorrowedByEmployeeName { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public string? CategoryName { get; set; }
        public ItemStatus Status { get; set; }
        public string TableName { get; set; } = string.Empty;
        public List<Employee> StorageEmployees { get; set; } = new List<Employee>();
        public List<Employee> BorrowingEmployees { get; set; } = new List<Employee>();

    }
}
