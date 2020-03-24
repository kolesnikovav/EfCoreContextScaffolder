using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AK.EFContextCommon
{
    public static class Helper
    {
        /// Adds all refferenced assembies into collection
        public static void ChekReferences(List<Assembly> list, Assembly asm)
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
        /// return ParameterAttributes from ParameterInfo
        public static ParameterAttributes GetParamAttributesFromParamInfo(ParameterInfo pI)
        {
            return (pI.HasDefaultValue ? ParameterAttributes.HasDefault : ParameterAttributes.None) |
            (pI.IsIn ? ParameterAttributes.In : ParameterAttributes.None) |
            (pI.IsLcid ? ParameterAttributes.Lcid : ParameterAttributes.None) |
            (pI.IsOptional ? ParameterAttributes.Optional : ParameterAttributes.None) |
            (pI.IsOut ? ParameterAttributes.Out : ParameterAttributes.None) |
            (pI.IsRetval ? ParameterAttributes.Retval : ParameterAttributes.None);
        }
        public static void TransferAttributes( PropertyBuilder typeBuilder, PropertyInfo typeInfo, List<Assembly> refsAsm)
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
        public static void TransferAttributes( ParameterBuilder typeBuilder, ParameterInfo typeInfo, List<Assembly> refsAsm)
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
        public static IEnumerable<Type> GetTypeHierarchy(Type t)
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
        public static IEnumerable<PropertyInfo> GetAllProps(Type t)
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
        public static IEnumerable<CustomAttributeData> GetTypeAttributes(Type t)
        {
            var cHierarchy = Helper.GetTypeHierarchy(t);
            List<CustomAttributeData> result = new List<CustomAttributeData>();
            foreach ( var cType in  cHierarchy)
            {
                foreach (var custAtr in cType.CustomAttributes)
                {
                    var idx = result.FindIndex(v => v.AttributeType == custAtr.AttributeType);
                    if (idx == -1) result.Add(custAtr);
                    else result[idx] = custAtr;
                }
            }
            return result;
        }
        public static void AddGetSetMethodsForProperty(PropertyInfo cProp, TypeBuilder createdType, List<Assembly> refsAsm)
        {
            FieldBuilder fieldBuilder = createdType.DefineField("_" + cProp.Name.ToLower(), cProp.PropertyType, FieldAttributes.Private);
            var pb = createdType.DefineProperty(cProp.Name, PropertyAttributes.None,CallingConventions.Standard, cProp.PropertyType,null);
            TransferAttributes(pb, cProp, refsAsm);
            if (cProp.GetGetMethod() != null)
            {
                MethodBuilder methodBuilderGet = createdType.DefineMethod("get_" + cProp.Name, MethodAttributes.Public);
                methodBuilderGet.SetReturnType(cProp.PropertyType);
                //create IL code for get
                ILGenerator genusGetIL = methodBuilderGet.GetILGenerator();
                genusGetIL.Emit(OpCodes.Ldarg_0);
                genusGetIL.Emit(OpCodes.Ldfld, fieldBuilder);
                genusGetIL.Emit(OpCodes.Ret);
                pb.SetGetMethod(methodBuilderGet);
            }
            if (cProp.GetSetMethod() != null)
            {
                MethodBuilder methodBuilderSet = createdType.DefineMethod("set_" + cProp.Name, MethodAttributes.Public);
                methodBuilderSet.SetParameters(new Type[] { cProp.PropertyType });
                //create IL code for set
                ILGenerator genusSetIL = methodBuilderSet.GetILGenerator();
                genusSetIL.Emit(OpCodes.Ldarg_0);
                genusSetIL.Emit(OpCodes.Ldarg_1);
                genusSetIL.Emit(OpCodes.Stfld, fieldBuilder);
                genusSetIL.Emit(OpCodes.Ret);
                pb.SetSetMethod(methodBuilderSet);
            }
        }
        public static void AddGetSetMethodsForProperty(string propName, Type propType, TypeBuilder createdType)
        {
            FieldBuilder fieldBuilder = createdType.DefineField("_" + propName.ToLower(), propType, FieldAttributes.Private);
            var pb = createdType.DefineProperty(propName,PropertyAttributes.None,CallingConventions.Standard,propType,new Type[1] { propType});
            MethodBuilder methodBuilderGet = createdType.DefineMethod("get_" + propName, MethodAttributes.Public);
            methodBuilderGet.SetReturnType(propType);
            //create IL code for get
            ILGenerator genusGetIL = methodBuilderGet.GetILGenerator();
            genusGetIL.Emit(OpCodes.Ldarg_0);
            genusGetIL.Emit(OpCodes.Ldfld, fieldBuilder);
            genusGetIL.Emit(OpCodes.Ret);
            pb.SetGetMethod(methodBuilderGet);

            MethodBuilder methodBuilderSet = createdType.DefineMethod("set_" + propName, MethodAttributes.Public);
            methodBuilderSet.SetParameters(new Type[] { propType });
            //create IL code for set
            ILGenerator genusSetIL = methodBuilderSet.GetILGenerator();
            genusSetIL.Emit(OpCodes.Ldarg_0);
            genusSetIL.Emit(OpCodes.Ldarg_1);
            genusSetIL.Emit(OpCodes.Stfld, fieldBuilder);
            genusSetIL.Emit(OpCodes.Ret);
            pb.SetSetMethod(methodBuilderSet);
        }
    }
}
