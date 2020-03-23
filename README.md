# EfCoreContextScaffolder
Create non-hierarchy entity models from hierarchy based entity classes. Try to implement (TPT) database storage type.

## How to use
 Create hierarchy db context class library
```cs
using System;
using System.ComponentModel.DataAnnotations;
namespace animals
{
    public class Animal
    {
        [Key]
        public Guid Id {get;set;}
        [MaxLength(11)]
        public string Code {get;set;}
        [MaxLength(100)]
        public string Name {get;set;}
    }
    public class Dog: Animal
    {
        [Required]
        [MaxLength(25)]
        public string Kind {get;set;}
        public int Age {get;set;}
    }
    public class Cat: Animal
    {
        [Required]
        [MaxLength(25)]
        public string Nick {get;set;}
        public int Age {get;set;}
    }
}
```
Include entity into db context

```cs
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
```
Currently, for this DB Context,EF Core creates one table and adds discriminator field into that.
Table-Per-Type storage type is not supported now. This program creates plain entity classes from this sample.
Inherited properties are included into each entity class. Thus, you can use inheritance and take advantages of simplifying entity describtion.

Build with all dependencies.
```bash
dotnet publish
```

Generate modified dll.
```bash
efdbcontextscaffolder -p <path to you dll>  -c <DBContext class name> -o <path to generated dll>
```
Then, generated dll you can include as refference into you project.

## Limitations

IdentityDBContext is not supported, because of dll dynamic loading problems.

## Thanks

Lokad.ILPack project.
https://github.com/Lokad/ILPack









