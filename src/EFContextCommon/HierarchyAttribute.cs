using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AK.EFContextCommon
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
        public static void Apply(Type t, AttributeTargets target, ModuleBuilder builder = null,  List<Assembly> refsAsm = null)
        {
            Type attrType = Type.GetType("AK.EFContextCommon.HierarchyAttribute");
            bool HasHierarchy = false;
            if (target == AttributeTargets.Class)
            {
                var pr = t.GetCustomAttributes(attrType,true);
                if (pr.Length > 0)
                HasHierarchy = (pr[0] as HierarchyAttribute).HasHierarchy;
                if (HasHierarchy && builder != null)
                {
                    var props = t.GetProperties();
                    List<PropertyInfo> propsToCopy = new List<PropertyInfo>();
                    foreach (var prop in props)
                    {
                        var PropertyAttributes = prop.GetCustomAttributes(attrType,true);
                        if (PropertyAttributes.Length > 0 && (PropertyAttributes[0] as HierarchyAttribute).HasHierarchy) propsToCopy.Add(prop);
                    }
                    // create class
                    string ClassName = String.IsNullOrWhiteSpace(t.Namespace) ? "" : t.Namespace + "."+t.Name + "Hierarchy";
                    var TypeBuilder = builder.DefineType(ClassName,TypeAttributes.Class| TypeAttributes.Public);
                    foreach( var prop in propsToCopy)
                    {
                        Helper.AddGetSetMethodsForProperty(prop, TypeBuilder , refsAsm);
                    }
                    TypeBuilder.CreateTypeInfo();
                }
            }
        }
    }

}