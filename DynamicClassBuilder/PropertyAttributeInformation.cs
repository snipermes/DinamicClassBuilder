using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicClassBuilder
{
    public class PropertyAttributeInformation
    {

        /// <summary>
        /// The attribute type
        /// </summary>
        public Type AttributeType;
        /// <summary>
        /// имя свойства атрибута, значение свойства атрибута (указываются в том же порядке , что и в конструкторе атрибута)
        /// </summary>
        public Dictionary<string, object> AttributeValues;
    }
}
