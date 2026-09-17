using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Digitale_Geraeteliste.Data.Repositories
{
    internal class ItemRepository : IItemRepository
    {
        private readonly LendContext _context;
        public ItemRepository(LendContext context)
        {
            _context = context;
        }
        
        public Item GetItemById(int id)
        {
            Item? itemById = _context.Items.Include(i => i.Category).FirstOrDefault(i => i.Id == id);
            return itemById ?? null!;
        }
        public IEnumerable<Item> GetAllItems()
        {
            return _context.Items.Include(i => i.Category).ToList();
        }
        public void ChangeItem(Item item, string inventoryNumber, string name, Category category, string description, int standardLendDuration, bool isRetired, bool needsMaintenance)
        {
            Item itemToChange = GetItemById(item.Id);
            itemToChange.InventoryNumber = inventoryNumber;
            itemToChange.Name = name;
            itemToChange.Category = category;
            itemToChange.CategoryId = category.Id;
            itemToChange.Description = description;
            itemToChange.StandardLendDuration = standardLendDuration;
            itemToChange.IsRetired = isRetired;
            itemToChange.NeedsMaintenance = needsMaintenance;
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex) // double it and give it to the next person
            {
                throw new Exception("An error occurred while updating the Database. Contact your system administrator.", ex);
            }
        }
        public bool CheckInventoryNumberDuplicate(string inventoryNumber, int itemId)
        {
            return _context.Items.Any(i => i.InventoryNumber == inventoryNumber && i.Id != itemId);
        }

        public void CreateNewItem(string inventoryNumber, string name, Category category, string description, int standardLendDuration, bool isRetired, bool needsMaintenance)
        {
            Item item = new Item(inventoryNumber,
                name,
                category,
                description,
                standardLendDuration,
                isRetired,
                needsMaintenance);
            _context.Items.Add(item);
            _context.SaveChanges();
        }
    }
}
