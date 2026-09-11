using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Digitale_Geraeteliste.Core.Model
{
    public class Item 
    {
        public int Id { get; set; }
        public int InventoryNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public Category? Category { get; set; } 
        public string Description { get; set; } = string.Empty;
        public int StandardLendDuration { get; set; }
        public bool IsActive { get; set; }
        public bool IsRetired { get; set; }
        public bool NeedsMaintenance { get; set; }

        private Item() { }
    }
}
