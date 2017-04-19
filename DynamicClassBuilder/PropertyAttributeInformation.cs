using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DynamicClassBuilder
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyAttributeInformation: Attribute
    {
        /// <summary>
        /// The attribute name
        /// </summary>
        public string Name;

        public PropertyAttributeInformation()
        {
        }

        public PropertyAttributeInformation(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The attribute type (if getting from hardcoded attributre class)
        /// </summary>
        public Type AttributeType {
            get { return mAttributeType; }
            set
            {
                if (value.BaseType==null || value.BaseType.Name != "Attribute")
                {
                    throw new ArgumentException(@"Заданный тип не является атрибутом (не наследован от него)",nameof(AttributeType));
                }
                mAttributeType = value;
                Name = mAttributeType.Name;
            }
        }

        private Type mAttributeType;
        /// <summary>
        /// имя свойства атрибута, значение свойства атрибута (указываются в том же порядке , что и в конструкторе атрибута)
        /// </summary>
        public Dictionary<string, object> AttributeValues=new Dictionary<string, object>();
    }
}
