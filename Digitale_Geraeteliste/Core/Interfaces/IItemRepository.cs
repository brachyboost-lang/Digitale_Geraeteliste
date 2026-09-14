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
        void SaveItem(Item item);
        bool CheckInventoryNumberDuplicate(string inventoryNumber);

    }
}
