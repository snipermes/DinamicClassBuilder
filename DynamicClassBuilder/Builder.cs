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
            mClassProperties = GetTypePublicProperties(propertiesFromType);
        }
        public Builder(string classSignatureName, object propertiesFromObject) : this(classSignatureName)
        {
            mClassProperties = GetObjectProperties(propertiesFromObject);
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
        /// <summary>
        /// Gets the object properties.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public List<PropertyInformation> GetObjectProperties(object obj)
        {
            var type = obj.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var dynProps = new List<PropertyInformation>();

            foreach (var prop in props)
            {
                var dynProp = new PropertyInformation
                {
                    PropertyName = prop.Name,
                    PropertyType = prop.PropertyType,
                    PropertyValue = prop.GetValue(obj, null),
                    CustomAttributes = GetCustomAttributes(prop)
                };
                dynProps.Add(dynProp);
            }
            return dynProps;
        }
        public Type GetResultObjectType(List<PropertyInformation> classProperties)
        {
            return mType ?? (mType = CompileResultType());
        }

        public object GetInstance(List<PropertyInformation> classProperties, bool setPropertyValue = true)
        {
            mClassProperties = classProperties;
            if (mType == null) mType = CompileResultType();
            mResult = Activator.CreateInstance(mType);
            if (!setPropertyValue) return mResult;
            foreach (var prop in mClassProperties)
                SetPropertyValue(prop);
            return mResult;
        }

        public IList GetGenericList()
        {
            if (mType == null) mType = CompileResultType();
            var type = typeof (List<>).MakeGenericType(mType);
            var instance = Activator.CreateInstance(type);
            return instance as IList;
        }
        /// <summary>
        /// Gets the type public properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public List<PropertyInformation> GetTypePublicProperties(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props.Select(GetPropertyInfo).ToList();
        }
        public PropertyInformation GetPropertyInfo(PropertyInfo prop)
        {
            var dynProp = new PropertyInformation
            {
                PropertyName = prop.Name,
                PropertyType = prop.PropertyType,
                CustomAttributes = GetCustomAttributes(prop)
            };
            return dynProp;
        }
        #endregion public Methods
        #region private methods
        /// <summary>
        /// Gets the custom attributes.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        private List<PropertyAttributeInformation> GetCustomAttributes(PropertyInfo property)
        {
            var customAttributes = new List<PropertyAttributeInformation>();
            var attrs = property.GetCustomAttributes(true);
            foreach (var atr in attrs)
            {
                var caProperties = atr.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var caValues = caProperties.Where(nValue => nValue.Name != "TypeId").ToDictionary(nValue => nValue.Name, nValue => nValue.GetValue(atr, null));
                var attribInfo = new PropertyAttributeInformation
                {
                    AttributeType = atr.GetType(),
                    AttributeValues = caValues
                };
                customAttributes.Add(attribInfo);
            }
            return customAttributes;
        }
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
                CreateProperty(field);
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
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(typeSignature + ".MainModule", classSignatureName + ".dll", true);
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
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

            PropertyInfo[] attrPropertyInfos = new PropertyInfo[attr.AttributeValues.Count];
            int i = 0;
            foreach (var attrProp in attr.AttributeValues)
            {

                types[i] = typeof(string);
                attrPropertyInfos[i] = attr.AttributeType.GetProperty(attrProp.Key);
                i++;
            }
            ConstructorInfo con = caType.GetConstructor(types);
            var attrValues = attr.AttributeValues.Select(x => x.Value).ToArray();
            if (con != null)
            {
                CustomAttributeBuilder stiAttrib = new CustomAttributeBuilder(con, new object[attr.AttributeValues.Count],
                    attrPropertyInfos, attrValues);
                propertyBuilder.SetCustomAttribute(stiAttrib);
            }
        }

        /// <summary>
        /// Creates the property.
        /// </summary>
        /// <param name="propertyInformation">The property information.</param>
        private void CreateProperty(PropertyInformation propertyInformation)
        {
            FieldBuilder fieldBuilder = mTypeBuilder.DefineField("_" + propertyInformation.PropertyName,
                propertyInformation.PropertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = mTypeBuilder.DefineProperty(propertyInformation.PropertyName,
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
