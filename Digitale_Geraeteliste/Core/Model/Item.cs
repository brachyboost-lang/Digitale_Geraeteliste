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

        public string InventoryNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Category Category { get; set; } = null!;
        public int CategoryId { get; set; } // FK
        public string Description { get; set; } = string.Empty;
        public int StandardLendDuration { get; set; }
        // public bool IsInUse { get; set; } retired, LendItem should be used to check if an item is in use
        public bool IsRetired { get; set; }
        public bool NeedsMaintenance { get; set; }
        public Item(string inventoryNumber, string name, Category category, string description, int standardLendDuration, bool isRetired, bool needsMaintenance)
        {
            Id = 0; // Id will be set by the database
            InventoryNumber = inventoryNumber;
            Name = name;
            Category = category;
            CategoryId = category.Id;
            Description = description;
            StandardLendDuration = standardLendDuration;
            IsRetired = isRetired;
            NeedsMaintenance = needsMaintenance;
        }

        private Item() { }
    }
}
