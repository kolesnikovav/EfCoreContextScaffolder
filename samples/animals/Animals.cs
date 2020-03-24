using System;
using System.ComponentModel.DataAnnotations;
using AK.EFContextCommon;

namespace animals
{
    [Hierarchy]
    public class Animal
    {
        [Key]
        [Hierarchy]
        public Guid Id {get;set;}
        [Hierarchy]
        [MaxLength(11)]
        public string Code {get;set;}
        [MaxLength(100)]
        [Hierarchy]
        public string Name {get;set;}
    }
    public class Dog: Animal
    {
        [Required]
        [MaxLength(25)]
        public string Kind {get;set;}

        public int Age {get;set;}
    }
    [Hierarchy(false)]
    public class Cat: Animal
    {
        [Required]
        [MaxLength(25)]
        public string Nick {get;set;}

        public int Age {get;set;}
    }
}
