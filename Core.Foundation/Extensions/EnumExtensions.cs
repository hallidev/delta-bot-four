using System;
using System.ComponentModel;

namespace Core.Foundation.Extensions
{
    /// <summary>
    /// Extensions for enums
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Returns the <see cref="DescriptionAttribute"/> for a given enum value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].Description;
            }

            return value.ToString();
        }
    }
}
