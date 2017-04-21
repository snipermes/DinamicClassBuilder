using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicClassBuilder
{
    public class Builder
    {
        #region private properties
        private List<PropertyInformation> mClassProperties = new List<PropertyInformation>();
        private Type mType;
        private readonly TypeBuilder mTypeBuilder;
        private ModuleBuilder mModuleBuilder;
        private object mResult;
        #endregion private properties
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Builder"/> class.
        /// </summary>
        /// <param name="classSignatureName">Name of the class signature.</param>
        public Builder(string classSignatureName)
        {
            mTypeBuilder = GetTypeBuilder(classSignatureName);

            ConstructorBuilder constructor =
                mTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                                      MethodAttributes.RTSpecialName);
        }
        public Builder(string classSignatureName, List<PropertyInformation> properties) : this(classSignatureName)
        {
            mClassProperties = properties;
        }
        public Builder(string classSignatureName, Type propertiesFromType) : this(classSignatureName)
        {
            mClassProperties = propertiesFromType.GetTypePublicProperties();
        }
        public Builder(string classSignatureName, object propertiesFromObject) : this(classSignatureName)
        {
            mClassProperties = propertiesFromObject.GetObjectProperties();
        }
        #endregion Constructor
        #region public properties


        public List<PropertyInformation> ClassProperties
        {
            get { return mClassProperties; }
            private set { mClassProperties = value; }
        }

        /// <summary>
        /// Gets the mResult object.
        /// </summary>
        /// <returns></returns>
        public Type Type
        {
            get { return mType; }
        }
        #endregion public properties
        #region public Methods

        public void AddProperty(PropertyInformation property)
        {
           // if(Type!=null) throw new InvalidOperationException(Properties.Resources.CantUseCompiledType);

            if (mClassProperties.Any(x => x.PropertyName == property.PropertyName))
            {
                //throw new InvalidOperationException(Properties.Resources.PropertyAllredyExists);
                var prop = mClassProperties.FirstOrDefault(x => x.PropertyName == property.PropertyName && x.PropertyType==property.PropertyType );
                if (prop == null) return;
                prop.PropertyValue = property.PropertyValue;
                prop.CustomAttributes = property.CustomAttributes;
            }
            else
            {
                mClassProperties.Add(property);
            }
            
        }
        public void AddProperty(PropertyInfo  property)
        {
            if (Type != null) throw new InvalidOperationException(Properties.Resources.CantUseCompiledType);
            if(mClassProperties.Any(x=>x.PropertyName==property.Name)) throw new InvalidOperationException(Properties.Resources.PropertyAllredyExists);
            mClassProperties.Add(property.GetPropertyInfo());
        }
 
        public Type GetResultObjectType(List<PropertyInformation> classProperties)
        {
            return mType ?? (mType = CompileResultType());
        }

        public object GetInstance(List<PropertyInformation> classProperties, bool setPropertyValue = true)
        {
            mClassProperties = classProperties;
            if (mClassProperties == null || mClassProperties.Count == 0)
            {
                throw new ArgumentNullException(nameof(classProperties), @"нет свойств");
            }
            if (mType == null) mType = CompileResultType();
            mResult = Activator.CreateInstance(mType);
            if (!setPropertyValue) return mResult;
            foreach (var prop in mClassProperties)
                SetPropertyValue(prop);
            return mResult;
        }

        public object GetInstance(bool setPropertyValue = true)
        {
            return GetInstance(mClassProperties, setPropertyValue);
        }

        public IList GetGenericList(List<object> objects =null)
        {
            if (mType == null) mType = CompileResultType();
            var type = typeof (List<>).MakeGenericType(mType);
            var instance = Activator.CreateInstance(type);
            var collection= instance as IList;
            if (objects == null) return collection;
            foreach (var obj in objects)
            {
                collection?.Add(obj);
            }
            return collection;
        }
        public IList GetGenericList(Type type)
        {
            if (type == null) return null;
            var t = typeof(List<>).MakeGenericType(type);
            var instance = Activator.CreateInstance(t);
            var collection = instance as IList;
            return collection;
        }

        #endregion public Methods
        #region private methods

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="property">The property.</param>
        private void SetPropertyValue(PropertyInformation property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            var prop = mResult.GetType()
                .GetProperty(property.PropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (null != prop && prop.CanWrite)
            {
                prop.SetValue(mResult, property.PropertyValue, null);
            }
        }

        /// <summary>
        /// Adds the properties.
        /// </summary>
        private void AddProperties()
        {
            foreach (var field in mClassProperties)
            {
                CreateProperty(field,mTypeBuilder);
            }
        }

        /// <summary>
        /// Compiles the type.
        /// </summary>
        /// <returns></returns>
        private Type CompileResultType()
        {
            AddProperties();
            Type objectType = mTypeBuilder.CreateType();
            return objectType;
        }


        /// <summary>
        /// Gets the type builder.
        /// </summary>
        /// <param name="classSignatureName">Name of the class signature.</param>
        /// <returns></returns>
        private TypeBuilder GetTypeBuilder(string classSignatureName = "DynamicType")
        {
            var typeSignature = classSignatureName;
            var an = new AssemblyName(typeSignature + ",Version=1.0.0.1");
            //генерация динамической сборки только с возможностью запуска
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an,
                AssemblyBuilderAccess.RunAndSave);
            mModuleBuilder = assemblyBuilder.DefineDynamicModule(typeSignature + ".MainModule", classSignatureName + ".dll", true);
            TypeBuilder tb = mModuleBuilder.DefineType(typeSignature,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                null);


            return tb;
        }



        /// <summary>
        /// Adds the custom attribute.
        /// </summary>
        /// <param name="propertyBuilder">The property builder.</param>
        /// <param name="attr">The attribute information.</param>
        private void AddCustomAttribute(PropertyBuilder propertyBuilder, PropertyAttributeInformation attr)
        {
            Type caType = attr.AttributeType;
            Type[] types = new Type[attr.AttributeValues.Count];
            var attrValues = attr.AttributeValues.Select(x => x.Value).ToArray();
            int i = 0;
            ConstructorInfo con;
            PropertyInfo[] attrPropertyInfos = new PropertyInfo[attr.AttributeValues.Count];
            foreach (var attrProp in attr.AttributeValues)
            {

                types[i] = attrProp.Value.GetType();
                if (attr.AttributeType != null) attrPropertyInfos[i] = attr.AttributeType.GetProperty(attrProp.Key);
                i++;
            }
            if (caType == null)
            {
                //return;
                con = CreateOwnAttributeConstructor(attr);
                CustomAttributeBuilder stiAttrib = new CustomAttributeBuilder(con, attrValues);
                propertyBuilder.SetCustomAttribute(stiAttrib);
            }
            else
            {
               
                 con = caType.GetConstructor(types);
                CustomAttributeBuilder stiAttrib = new CustomAttributeBuilder(con,attrValues);
                propertyBuilder.SetCustomAttribute(stiAttrib);
            }


        }

        private ConstructorInfo CreateOwnAttributeConstructor(PropertyAttributeInformation attr)
        {
            //return null;
            var typeSignature = attr.Name ;
            //var an = new AssemblyName(typeSignature + ",Version=1.0.0.1");
            ////генерация динамической сборки только с возможностью запуска
            //AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an,
            //    AssemblyBuilderAccess.RunAndSave);
            //ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(typeSignature + ".MainModule", typeSignature + ".dll", true);
            var moduleTypes = mModuleBuilder.GetTypes().ToList();
            //todo проверить список типо на наличие уже сгенерированного
            //todo и вообще сделать это на уровень выше и если тип уже есть то идти по стандартному пути получения конструктора
            //todo а ещё лучше тут не конструктор получать а генерировать и задавать тип для PropertyAttributeInformation

            if (moduleTypes.FirstOrDefault(x => x.Name == typeSignature) == null)
            {
            }
            TypeBuilder tb = mModuleBuilder.DefineType(typeSignature,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                null);
            
            tb.SetParent(typeof(Attribute));
            
                tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                                      MethodAttributes.RTSpecialName);

            foreach (var val in attr.AttributeValues)
            {
                var field = new PropertyInformation
                {
                    PropertyName = val.Key,
                    PropertyValue = val.Value,
                    PropertyType = val.Value.GetType()
                };


                CreateProperty(field, tb);
            }
            Type[] types = new Type[attr.AttributeValues.Count];
            int i = 0;
            foreach (var attrProp in attr.AttributeValues)
            {
                types[i] = attrProp.Value.GetType();
                i++;
            }
            var constructor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, types);
            ILGenerator myConstructorIL = constructor.GetILGenerator();
            myConstructorIL.Emit(OpCodes.Ldarg_0);
            myConstructorIL.Emit(OpCodes.Ldarg_1);
            myConstructorIL.Emit(OpCodes.Stfld, attr.AttributeValues.FirstOrDefault().Key);
            myConstructorIL.Emit(OpCodes.Ret);
            return constructor as ConstructorInfo;


        }

        /// <summary>
        /// Creates the property.
        /// </summary>
        /// <param name="propertyInformation">The property information.</param>
        private void CreateProperty(PropertyInformation propertyInformation, TypeBuilder typeBuilder)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propertyInformation.PropertyName,
                propertyInformation.PropertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyInformation.PropertyName,
                PropertyAttributes.None, propertyInformation.PropertyType, null);

            //Добавление аттрибут к свойству
            if (propertyInformation.CustomAttributes != null) foreach (var cusAttr in propertyInformation.CustomAttributes) AddCustomAttribute(propertyBuilder, cusAttr);

            propertyBuilder.SetGetMethod(MakeGetMethod(fieldBuilder, propertyInformation));
            propertyBuilder.SetSetMethod(MakeSetMethod(fieldBuilder, propertyInformation));
        }

        /// <summary>
        /// Makes the set method.
        /// </summary>
        /// <param name="fieldBuilder">The field builder.</param>
        /// <param name="propertyInformation">The property information.</param>
        /// <returns></returns>
        private MethodBuilder MakeSetMethod(FieldBuilder fieldBuilder, PropertyInformation propertyInformation)
        {
            MethodBuilder setPropMthdBldr =
                mTypeBuilder.DefineMethod("set_" + propertyInformation.PropertyName,
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    null, new[] { propertyInformation.PropertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            //if (fieldBuilder.FieldType.IsValueType)
            //    setIl.Emit(OpCodes.Box, fieldBuilder.FieldType);
            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);
            return setPropMthdBldr;
        }

        /// <summary>
        /// Makes the get method.
        /// </summary>
        /// <param name="fieldBuilder">The field builder.</param>
        /// <param name="propertyInformation">The property information.</param>
        /// <returns></returns>
        private MethodBuilder MakeGetMethod(FieldBuilder fieldBuilder, PropertyInformation propertyInformation)
        {
            MethodBuilder getPropMthdBldr = mTypeBuilder.DefineMethod(
                "get_" + propertyInformation.PropertyName,
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig,
                propertyInformation.PropertyType,
                Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            return getPropMthdBldr;
        }
        #endregion private methods



    }
}
