using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Digitale_Geraeteliste.Data.Repositories
{
    internal class ItemRepository : IItemRepository
    {
        private LendContext Context;
        public ItemRepository() 
        {
            Context = new LendContext();
        }
        public Item GetItemById(int id)
        {
            var allItems = GetAllItems();
            return allItems.FirstOrDefault(i => i.Id == id);
        }
        public IEnumerable<Item> GetAllItems()
        {
            return Context.Items.Include(i => i.Name).ToList();
        }
        public void ChangeItem(Item item, string inventoryNumber, string name, Category category, string description, int standardLendDuration, bool isInUse, bool isRetired, bool needsMaintenance)
        {
            item.InventoryNumber = inventoryNumber;
            item.Name = name;
            item.Category = category;
            item.CategoryId = category.Id;
            item.Description = description;
            item.StandardLendDuration = standardLendDuration;
            item.IsInUse = isInUse;
            item.IsRetired = isRetired;
            item.NeedsMaintenance = needsMaintenance;
            SaveItem(item);
        }
        public bool CheckInventoryNumberDuplicate(string inventoryNumber)
        {
            var allItems = GetAllItems();
            return allItems.Any(i => i.InventoryNumber == inventoryNumber);
        }

        public void SaveItem(Item item)
        {
            
        }
    }
}
