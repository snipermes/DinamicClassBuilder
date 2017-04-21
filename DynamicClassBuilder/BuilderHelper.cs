using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicClassBuilder
{
    public static class BuilderHelper
    {
        /// <summary>
        /// Gets the object properties.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static List<PropertyInformation> GetObjectProperties(this object obj)
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
        /// <summary>
        /// Gets the type public properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static List<PropertyInformation> GetTypePublicProperties(this Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props.Select(GetPropertyInfo).ToList();
        }
        public static PropertyInformation GetPropertyInfo(this PropertyInfo prop)
        {
            var dynProp = new PropertyInformation
            {
                PropertyName = prop.Name,
                PropertyType = prop.PropertyType,
                CustomAttributes = GetCustomAttributes(prop)
            };
            return dynProp;
        }
        /// <summary>
        /// Возвращает значение указанного свойства объекта
        /// </summary>
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            foreach (var prop in propertyName.Split('.'))
            {
                if (obj == null) return null;
                var property = obj.GetType().GetProperty(prop);
                obj = prop == null ? null : property.GetValue(obj, null);
            }
            return obj;
        }

        public static void SetPropertyValue<T>(this T source, string propertyName, object value)
        {
            var prop= source.GetType().GetProperty(propertyName);
            prop?.SetMethod.Invoke(source, new [] { value });
        }

        /// <summary>
        /// Gets the custom attributes.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        private static List<PropertyAttributeInformation> GetCustomAttributes(this PropertyInfo property)
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

        public static List<PropertyInformation> OrderByCustomAttributeProperty(this List<PropertyInformation> properties, string attributeName,
            string attributePropertyName)
        {
            
            if (properties.All(
                    x =>
                        x.CustomAttributes != null && x.CustomAttributes.Count>0 &&
                        x.CustomAttributes.Select(name => name.Name).Contains(attributeName) &&
                        x.CustomAttributes.All(atr=>atr.AttributeValues.ContainsKey(attributePropertyName))))
            {
                return
                    properties.OrderBy(
                        x =>
                            x.CustomAttributes.Where(atr => atr.Name == attributeName)
                                .Select(v => v.AttributeValues[attributePropertyName])
                                .First()).ToList();
            }
            return properties;
            throw new ArgumentNullException(nameof(attributeName), @"Attribute or attribute property not found");
        }
        
    }
}
