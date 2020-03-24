using System;
using System.ComponentModel.DataAnnotations;

namespace animals
{
    [AttributeUsage(AttributeTargets.Class| AttributeTargets.Property)]
    public class HierarchyAttribute: Attribute
    {
        public bool HasHierarchy{get;set;}
        public HierarchyAttribute(bool val)
        {
            this.HasHierarchy = val;
        }
        public HierarchyAttribute()
        {
            this.HasHierarchy = true;
        }
        public static void Apply()
        {

        }

    }

}