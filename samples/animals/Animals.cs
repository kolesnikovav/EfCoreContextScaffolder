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
