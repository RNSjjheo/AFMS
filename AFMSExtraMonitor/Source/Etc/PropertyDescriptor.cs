using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AFMSExtraMonitor
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PropertyOrderAttribute : Attribute
    {
        public int Order { get; }

        public PropertyOrderAttribute(int order)
        {
            Order = order;
        }
    }

    public sealed class PropertyOrderConverter : ExpandableObjectConverter
    {
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(value, attributes, true);

            PropertyDescriptor[] sortedProperties = properties.Cast<PropertyDescriptor>().OrderBy(GetOrder).ThenBy(p => p.DisplayName).ToArray();

            return new PropertyDescriptorCollection(sortedProperties, true);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context)
        {
            return true;
        }

        private static int GetOrder(PropertyDescriptor property)
        {
            PropertyOrderAttribute? orderAttribute = property.Attributes[typeof(PropertyOrderAttribute)] as PropertyOrderAttribute;

            return orderAttribute?.Order ?? int.MaxValue;
        }
    }
}
