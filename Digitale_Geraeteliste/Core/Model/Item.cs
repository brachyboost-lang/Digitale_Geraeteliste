using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Digitale_Geraeteliste.Core.Model
{
    internal class Item : Category
    {
        internal int ItemId { get; set; }
        internal int InventoryNumber { get; set; }
        internal string ItemName { get; set; } = string.Empty;
        internal string Description { get; set; } = string.Empty;
        internal int StandardLendDuration { get; set; }
        internal bool IsActive { get; set; }
        internal bool IsRetired { get; set; }
        internal bool NeedsMaintenance { get; set; }
    }
}
