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
        private readonly LendContext Context;
        public ItemRepository(LendContext context)
        {
            Context = context;
        }
        public Item GetItemById(int id)
        {
            try
            {
                var allItems = GetAllItems();
                Item itemById = allItems.First(i => i.Id == id);
                return itemById;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving the item by ID. Check for Typo or Item might not exist.", ex);
            }
        }
        public IEnumerable<Item> GetAllItems()
        {
            return Context.Items.Include(i => i.Category).ToList();
        }
        public void ChangeItem(Item item, string inventoryNumber, string name, Category category, string description, int standardLendDuration, bool isInUse, bool isRetired, bool needsMaintenance)
        {
            Item itemToChange = GetItemById(item.Id);
            itemToChange.InventoryNumber = inventoryNumber;
            itemToChange.Name = name;
            itemToChange.Category = category;
            itemToChange.CategoryId = category.Id;
            itemToChange.Description = description;
            itemToChange.StandardLendDuration = standardLendDuration;
            itemToChange.IsInUse = isInUse;
            itemToChange.IsRetired = isRetired;
            itemToChange.NeedsMaintenance = needsMaintenance;
            try
            {
            UpdateItem(itemToChange);
            }
            catch (Exception ex) // double it and give it to the next person
            {
                throw new Exception("An error occurred while updating the Database. Contact your system administrator.", ex); 
            }
        }
        public bool CheckInventoryNumberDuplicate(string inventoryNumber)
        {
            var allItems = GetAllItems();
            return allItems.Any(i => i.InventoryNumber == inventoryNumber);
        }

        public void UpdateItem(Item item)
        {
            Context.Items.Update(item);
            Context.SaveChanges();
        }
    }
}
