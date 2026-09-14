using Digitale_Geraeteliste.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Digitale_Geraeteliste.Data.Repositories
{
    public class LendContext : DbContext
    {
        public DbSet<Item> Items => Set<Item>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<LendItem> LendItems => Set<LendItem>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Digitale_Geraeteliste.db");
                options.UseSqlite($"Data Source={path}");
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder) { }
    }
}
