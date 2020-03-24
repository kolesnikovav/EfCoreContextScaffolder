using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Lokad.ILPack;
using AK.EFContextCommon;

namespace AK.EFContextCommon.Console
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
                        string ClassName = String.IsNullOrWhiteSpace(t.Namespace) ? "" : t.Namespace + "."+t.Name;
                        var createdTypeDBContext = myModBuilder.DefineType(ClassName,TypeAttributes.Class | TypeAttributes.Public);

                        var DBContextClass = new DBContextContent();
                        DBContextClass.ClassName = ClassName;
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
                            string entityClassName = String.IsNullOrWhiteSpace(entityType.Namespace) ? "" : entityType.Namespace + "."+entityType.Name;
                            var createdType = myModBuilder.DefineType(entityClassName,TypeAttributes.Class | TypeAttributes.Public);
                            var propsFlattern = Helper.GetAllProps(entityType); //GetAllProps(entityType);
                            var allEntityAttributes = Helper.GetTypeAttributes(entityType);

                            foreach (var atr in allEntityAttributes) // entity attributes
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
                                object[] cArgs = new object[atrArgs.Count()];
                                for (int i=0; i < atrArgs.Count(); i++)
                                {
                                    cArgs[i] = (object)atrArgs[i];
                                }
                                var cTors = atr.AttributeType.GetConstructors();
                                var constructorForAttribute = cTors.FirstOrDefault(v => v.GetParameters().Count() == atrArgs.Count());
                                try
                                {
                                    CustomAttributeBuilder cA = new CustomAttributeBuilder(constructorForAttribute, cArgs);
                                    createdType.SetCustomAttribute(cA);
                                }
                                catch (Exception e)
                                {
                                    //
                                }
                                // some actions when attribute is present!
                                var memberAttributes = atr.AttributeType.GetMethods();
                                foreach (var mA in memberAttributes.Where( v => v.Name == "Apply"))
                                {
                                    var args = new object[] {entityType, AttributeTargets.Class, myModBuilder, refsAsm};
                                    mA.Invoke(null, args);
                                }
                            }
                            foreach (var cProp in propsFlattern)
                            {
                                Helper.AddGetSetMethodsForProperty(cProp, createdType, refsAsm);
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
                        Helper.AddGetSetMethodsForProperty(contProps.PropName, tDBsetType, tb);
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