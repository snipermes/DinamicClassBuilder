using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicClassBuilder
{
    public class PropertyInformation
    {
        /// <summary>
        /// The property name
        /// </summary>
        public string PropertyName;
        /// <summary>
        /// The description
        /// </summary>
        public string Description;
        /// <summary>
        /// The property type
        /// </summary>
        public Type PropertyType;
        /// <summary>
        /// The property value
        /// </summary>
        public object PropertyValue;
        /// <summary>
        /// The custom attributes
        /// </summary>
        public List<PropertyAttributeInformation> CustomAttributes=new List<PropertyAttributeInformation>();
    }
}
