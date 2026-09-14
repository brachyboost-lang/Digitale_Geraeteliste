using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Data.Repositories
{
    public class LendContext : DbContext
    {
        public DbSet<Item> Items => Set<Item>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<LendItem> LendItems => Set<LendItem>();

        protected override void OnConfiguring(DbContextOptionsBuilder options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder) { }

        public LendContext()
        {

        }
    }
}
