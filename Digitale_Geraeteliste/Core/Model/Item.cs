using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml;

namespace Digitale_Geraeteliste.Core.Model
{
    public class Item 
    {
        public int Id { get; set; } // PK

        public required string InventoryNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public Category? Category { get; set; } 
        public int CategoryId { get; set; } // FK
        public string Description { get; set; } = string.Empty;
        public int StandardLendDuration { get; set; }
        public bool IsInUse { get; set; }
        public bool IsRetired { get; set; }
        public bool NeedsMaintenance { get; set; }
        public Item(string inventoryNumber, string name, Category category, string description, int standardLendDuration, bool isInUse, bool isRetired, bool needsMaintenance)
        {
            InventoryNumber = inventoryNumber;
            Name = name;
            Category = category;
            Description = description;
            StandardLendDuration = standardLendDuration;
            IsInUse = isInUse;
            IsRetired = isRetired;
            NeedsMaintenance = needsMaintenance;
        }

        private Item() { }
    }
}
