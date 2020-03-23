using System;
using Microsoft.EntityFrameworkCore;

namespace animals
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Dog> Dogs {get;set;}
        public DbSet<Cat> Cats {get;set;}

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
    }
}