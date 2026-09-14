using Digitale_Geraeteliste.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Digitale_Geraeteliste.Core.Interfaces
{
    public interface IItemRepository
    {
        public abstract Item GetItemById(int id);
        public abstract IEnumerable<Item> GetAllItems();
        public abstract void SaveItem(Item item);
        public abstract bool CheckInventoryNumberDuplicate(string inventoryNumber);

    }
}
