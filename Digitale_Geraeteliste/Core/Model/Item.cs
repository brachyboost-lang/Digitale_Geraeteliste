using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Core.Model
{
    internal class Item
    {
        public int Id { get; set; }
        public int InventoryNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int standardLendDuration { get; set; }
        public bool IsActive { get; set; }
        public bool IsRetired { get; set; }
        public bool NeedsMaintenance { get; set; }
    }
}
