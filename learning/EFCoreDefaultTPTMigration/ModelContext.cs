using EFCoreTPCMigration.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace EFCoreTPCMigration
{
    public class ModelContext : DbContext
    {

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(con =>
            {
                
            });
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Identity>().UseTpcMappingStrategy()
                .ToTable("Identities").HasKey("Id");

            modelBuilder.Entity<SecurityGroup>().ToTable("SecurityGroups");
            modelBuilder.Entity<User>().ToTable("Users");

        }
    }
}
