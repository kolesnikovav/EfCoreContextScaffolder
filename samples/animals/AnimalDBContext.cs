using System;
using Microsoft.EntityFrameworkCore;

namespace animals
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Dog> Dogs {get;set;}
        public DbSet<Cat> Cats {get;set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
    }
}