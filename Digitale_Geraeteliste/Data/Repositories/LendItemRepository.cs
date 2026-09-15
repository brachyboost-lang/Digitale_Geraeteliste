using Digitale_Geraeteliste.Core.Interfaces;
using Digitale_Geraeteliste.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Digitale_Geraeteliste.Data.Repositories
{
    internal class LendItemRepository : ILendItemRepository
    {
        private readonly LendContext _context;
        public LendItemRepository(LendContext context)
        {
            _context = context;
        }
        public void ChangeLendItem(LendItem lendItem, int itemId, int employeeId, DateTime lendDate, DateTime returnDate)
        {
            throw new NotImplementedException();
        }

        public LendItem CreateNewLendItem(int itemId, int employeeId, DateTime lendDate, DateTime returnDate)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<LendItem> GetAllLendItems()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<LendItem> GetAllOverdueLendItems()
        {
            throw new NotImplementedException();
        }

        public LendItem GetLendItemById(int id)
        {
            throw new NotImplementedException();
        }

        public void ReturnLendItem(int lendItemId, DateTime returnDate)
        {
            throw new NotImplementedException();
        }
    }
}
