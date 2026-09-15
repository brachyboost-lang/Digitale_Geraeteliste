using Digitale_Geraeteliste.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface IItemRepository
    {
        Item GetItemById(int id);
        IEnumerable<Item> GetAllItems();
        void ChangeItem(Item item, string inventoryNumber, string name, Category category, string description, int standardLendDuration, bool isRetired, bool needsMaintenance);
        bool CheckInventoryNumberDuplicate(string inventoryNumber, string name);
    }
}
