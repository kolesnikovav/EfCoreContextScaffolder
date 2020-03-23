using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Lokad.ILPack;

namespace EFCoreDBContextScaffolder
{
    public class DBContextContent
    {
        public string ClassName {get;set;}
        public TypeBuilder TypeBuilder {get;set;}
        public List<EntityDescribtion> Content {get;set;}
        public DBContextContent()
        {
            this.Content = new List<EntityDescribtion>();
        }
    }
    public class EntityDescribtion
    {
        public string PropName {get;set;}
        public Type PropType {get;set;}
        public Type OriginalPropType {get;set;}
        public Type EntityType {get;set;}
    }
    public class Scaffolder
    {
        private string path;
        private string output;
        private string dbContextType = "Microsoft.EntityFrameworkCore.DbContext";
        private string dbObjecttType = "System.Object";
        private bool IsDBContextType(Type t)
        {
            var tb = t.BaseType;
            if (tb == null || tb.FullName == dbObjecttType) return false;
            while (true)
            {
                if (tb == null) return false;
                if (tb.FullName == dbContextType) return true;
                tb = tb.BaseType;
            }
        }

        private List<PropertyInfo> GetAllProps(Type t)
        {
            var result = new List<PropertyInfo>();
            result.AddRange(t.GetProperties());
            return result;
        }
        private void AddGetSetMethodsForProperty(PropertyBuilder pb, PropertyInfo cProp, TypeBuilder createdType, FieldBuilder fieldBuilder)
        {
            MethodBuilder methodBuilderGet = createdType.DefineMethod("get_" + cProp.Name, MethodAttributes.Public);
            methodBuilderGet.SetReturnType(cProp.PropertyType);
            //create IL code for get
            ILGenerator genusGetIL = methodBuilderGet.GetILGenerator();
            genusGetIL.Emit(OpCodes.Ldarg_0);
            genusGetIL.Emit(OpCodes.Ldfld, fieldBuilder);
            genusGetIL.Emit(OpCodes.Ret);
            pb.SetGetMethod(methodBuilderGet);

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

        private void AddGetSetMethodsForProperty(PropertyBuilder pb, string propName, Type propType, TypeBuilder createdType, FieldBuilder fieldBuilder)
        {
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
        internal ModuleResolveEventHandler OnResolve()
        {
            return null;
        }

        public void ReadAssembly ()
        {
            if (!File.Exists(path)) throw new Exception(String.Format("File {0} not exists!",this.path));
            AppDomain myDomain = Thread.GetDomain();
            AssemblyName myAsmName = new AssemblyName();
            myAsmName.Name = Path.GetFileNameWithoutExtension(path)+".proxy.dll";

            AssemblyBuilder myAsmBuilder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,AssemblyBuilderAccess.Run);
            // Generate a persistable single-module assembly.
            ModuleBuilder myModBuilder =
                  myAsmBuilder.DefineDynamicModule(myAsmName.Name + ".dll");
            using (var dynamicContext = new AssemblyResolver(path))
            {
                List<Assembly> refsAsm = new List<Assembly>();
                List<Type> EntityTypes = new List<Type>();
                List<DBContextContent> DBContextClasses = new List<DBContextContent>();
                var dynAssembly = dynamicContext.Assembly;
                var aTypes = dynAssembly.GetTypes();

                foreach(var t in aTypes)
                {
                    if (IsDBContextType(t))
                    {
                        var createdTypeDBContext = myModBuilder.DefineType(t.Name,TypeAttributes.Class | TypeAttributes.Public);

                        var DBContextClass = new DBContextContent();
                        DBContextClass.ClassName = t.Name;
                        DBContextClasses.Add(DBContextClass);
                        createdTypeDBContext.SetParent(t.BaseType);
                        // copy constructor && methods
                        var dbContextConstructors = t.GetConstructors(BindingFlags.DeclaredOnly);
                        foreach (var cI in dbContextConstructors)
                        {
                            MethodAttributes visibility = cI.IsStatic ? MethodAttributes.Static : MethodAttributes.Public;
                            var cBuilder = createdTypeDBContext.DefineConstructor(visibility,CallingConventions.Standard,null);
                            foreach (var cParams in cI.GetParameters())
                            {
                                var cP = cBuilder.DefineParameter(cParams.Position,Helper.GetParamAttributesFromParamInfo(cParams),cParams.Name);
                                cP.SetConstant(cParams.DefaultValue);
                                Helper.TransferAttributes( cP, cParams, refsAsm);
                            }
                        }
                        var dbContextMethods = t.GetMethods(BindingFlags.DeclaredOnly);
                        foreach (var cI in dbContextMethods)
                        {
                            MethodAttributes visibility = cI.IsStatic ? MethodAttributes.Static : MethodAttributes.Public;
                            var cBuilder = createdTypeDBContext.DefineMethod(cI.Name,visibility);
                            foreach (var cParams in cI.GetParameters())
                            {
                                var cP = cBuilder.DefineParameter(cParams.Position,Helper.GetParamAttributesFromParamInfo(cParams),cParams.Name);
                                cP.SetConstant(cParams.DefaultValue);
                                Helper.TransferAttributes( cP, cParams, refsAsm);
                            }
                        }
                        DBContextClass.TypeBuilder = createdTypeDBContext;
                        // - copy constructor & methods
                        PropertyInfo[] props = t.GetProperties();
                        foreach (var dbContextMember in props)
                        {
                            Type cType = dbContextMember.PropertyType;
                            Type[] gArgs = cType.GenericTypeArguments;
                            if (cType != null && cType.IsGenericType && cType.FullName.StartsWith("Microsoft.EntityFrameworkCore.DbSet") && gArgs.Length > 0)
                            {
                                if (EntityTypes.Find(v => (v != null && v.FullName == gArgs[0].FullName)) == null) {
                                    EntityTypes.Add(gArgs[0]);
                                    EntityDescribtion eDescr  = new EntityDescribtion();
                                    eDescr.EntityType = gArgs[0];
                                    eDescr.PropName = dbContextMember.Name;
                                    //eDescr.OriginalPropType = cType.ReflectedType.BaseType;
                                    DBContextClass.Content.Add(eDescr);
                                }
                            }
                        }
                        foreach(var entityType in EntityTypes)
                        {
                            var createdType = myModBuilder.DefineType(entityType.Name,TypeAttributes.Class | TypeAttributes.Public);
                            var propsFlattern = Helper.GetAllProps(entityType); //GetAllProps(entityType);
                            foreach (var atr in entityType.CustomAttributes) // entity attributes
                            {
                                var classAtributes = atr.AttributeType.GetCustomAttributesData();
                                object[] atribArgs = new object[classAtributes.Count];
                                for(var i = 0; i<  classAtributes.Count; i++)
                                {
                                    atribArgs[i] = classAtributes[i];
                                    var atrType = classAtributes[i].AttributeType;
                                    Helper.ChekReferences(refsAsm,atrType.Assembly);
                                }
                                var  atrArgs = atr.ConstructorArguments.ToArray();
                                CustomAttributeBuilder cA = new CustomAttributeBuilder(atr.Constructor, atribArgs);
                                createdType.SetCustomAttribute(cA);
                            }
                            foreach (var cProp in propsFlattern)
                            {
                                FieldBuilder fieldBuilder = createdType.DefineField("_" + cProp.Name.ToLower(), cProp.PropertyType, FieldAttributes.Private);
                                var pb = createdType.DefineProperty(cProp.Name, PropertyAttributes.None,CallingConventions.Standard, cProp.PropertyType,null);
                                Helper.TransferAttributes(pb, cProp, refsAsm);

                                AddGetSetMethodsForProperty( pb,  cProp, createdType, fieldBuilder);
                            }
                            var dbContextType = createdType.CreateType();
                            var eDescr = DBContextClass.Content.First(e => e.EntityType == entityType);
                            if (eDescr != null)
                            {
                                eDescr.PropType = dbContextType; // generetad type
                            }
                        }
                    }

                }
                foreach (var cont in DBContextClasses)
                {
                    var tb = cont.TypeBuilder;
                    foreach (var contProps in cont.Content)
                    {
                        var tp = contProps.EntityType;
                        var tDBSet = typeof(Microsoft.EntityFrameworkCore.DbSet<>);
                        var tDBsetType = tDBSet.MakeGenericType(contProps.PropType);
                        if (refsAsm.FindIndex(v => v.FullName ==tDBsetType.Assembly.FullName) == -1) {
                         var asm = new AssemblyResolver(tDBsetType.Assembly.Location);
                             refsAsm.Add(tDBSet.Assembly);
                        }
                        FieldBuilder fieldBuilder = tb.DefineField("_" + contProps.PropName.ToLower(), tDBsetType, FieldAttributes.Private);
                        var pb = tb.DefineProperty(contProps.PropName,PropertyAttributes.None,CallingConventions.Standard,tDBsetType,new Type[1] { tDBsetType});
                        AddGetSetMethodsForProperty( pb, contProps.PropName, tDBsetType , tb, fieldBuilder);
                    }
                    tb.CreateType();
                }
                var generator = new AssemblyGenerator();
                generator.GenerateAssembly(myAsmBuilder,refsAsm, output);
            }

        }

        public Scaffolder(string Path, string Output)
        {
            this.path = Path;
            this.output = Output;
        }
    }
}