using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace EFCoreDBContextScaffolder
{
    internal static class Helper
    {
        internal static ParameterAttributes GetParamAttributesFromParamInfo(ParameterInfo pI)
        {
            return (pI.HasDefaultValue ? ParameterAttributes.HasDefault : ParameterAttributes.None) |
            (pI.IsIn ? ParameterAttributes.In : ParameterAttributes.None) |
            (pI.IsLcid ? ParameterAttributes.Lcid : ParameterAttributes.None) |
            (pI.IsOptional ? ParameterAttributes.Optional : ParameterAttributes.None) |
            (pI.IsOut ? ParameterAttributes.Out : ParameterAttributes.None) |
            (pI.IsRetval ? ParameterAttributes.Retval : ParameterAttributes.None);
        }
        internal static void TransferAttributes( PropertyBuilder typeBuilder, PropertyInfo typeInfo, List<Assembly> refsAsm)
        {
            foreach (var atr in typeInfo.CustomAttributes)
            {
                ChekReferences( refsAsm, atr.AttributeType.Assembly);
                var propAtributeData = atr.ConstructorArguments;
                object[] atribArgsProp = new object[propAtributeData.Count];
                for (var i = 0; i < propAtributeData.Count; i++)
                {
                    atribArgsProp[i] = propAtributeData[i].Value;
                    ChekReferences( refsAsm, propAtributeData[i].Value.GetType().Assembly);
                }
                CustomAttributeBuilder cA = new CustomAttributeBuilder(atr.Constructor, atribArgsProp);
                ChekReferences( refsAsm, atr.Constructor.GetType().Assembly);
                typeBuilder.SetCustomAttribute(cA);
            }
        }
        internal static void TransferAttributes( ParameterBuilder typeBuilder, ParameterInfo typeInfo, List<Assembly> refsAsm)
        {
            foreach (var atr in typeInfo.CustomAttributes)
            {
                ChekReferences( refsAsm, atr.AttributeType.Assembly);
                var propAtributeData = atr.ConstructorArguments;
                object[] atribArgsProp = new object[propAtributeData.Count];
                for (var i = 0; i < propAtributeData.Count; i++)
                {
                    atribArgsProp[i] = propAtributeData[i].Value;
                    ChekReferences( refsAsm, propAtributeData[i].Value.GetType().Assembly);
                }
                CustomAttributeBuilder cA = new CustomAttributeBuilder(atr.Constructor, atribArgsProp);
                ChekReferences( refsAsm, atr.Constructor.GetType().Assembly);
                typeBuilder.SetCustomAttribute(cA);
            }
        }
        internal static void ChekReferences(List<Assembly> list, Assembly asm)
        {
            var refAsmArray = asm.GetReferencedAssemblies();
            foreach (var asmForAdd in refAsmArray)
            {
                Assembly asmCheck = Assembly.Load(asmForAdd);
                if (list.FirstOrDefault(v => v.FullName == asmCheck.FullName) == null)
                {
                    list.Add(asmCheck);
                }
            }
            if (list.FirstOrDefault(v => v.FullName == asm.FullName) == null)
            {
                list.Add(asm);
            }
        }
        internal static List<Type> GetTypeHierarchy(Type t)
        {
            var res = new Dictionary<int,Type>();
            var i = 0;
            var baseType = t;
            while (true)
            {
                res.Add(i,baseType);
                i++;
                baseType = baseType.BaseType;
                if (baseType == null || baseType.FullName == "System.Object") break;
            }
            var result = new List<Type>();
            foreach(var r in res.OrderByDescending(v => v.Key))
            {
                result.Add(r.Value);
            }
            return result;
        }
        internal static List<PropertyInfo> GetAllProps(Type t)
        {
            var result = new List<PropertyInfo>();
            var thierarchy = GetTypeHierarchy(t);
            foreach( var cType in thierarchy)
            {
                var props = cType.GetProperties();
                foreach( var cp in props)
                {
                    var fndPropIndex = result.FindIndex(0,result.Count, v => v.Name == cp.Name);
                    if (fndPropIndex == -1)
                    {
                        result.Add(cp);
                    }
                    else
                    {
                        result[fndPropIndex] = cp;
                    }
                }
            }
            return result;
        }
    }

}