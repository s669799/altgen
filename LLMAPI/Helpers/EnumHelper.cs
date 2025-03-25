using System;
using System.Runtime.Serialization;
using System.Reflection;

namespace LLMAPI.Helpers
{
    /// <summary>
    /// Helper class providing utility methods for working with enums.
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Retrieves the value of the <see cref="EnumMemberAttribute"/> for a given enum value.
        /// If the attribute is not present, it returns the enum value's string representation.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="enumValue">The enum value to get the member value for.</param>
        /// <returns>The value specified in the <see cref="EnumMemberAttribute"/>, or the enum value's name if the attribute is not present.</returns>
        public static string GetEnumMemberValue<T>(T enumValue) where T : Enum
        {
            FieldInfo field = typeof(T).GetField(enumValue.ToString());
            EnumMemberAttribute attribute = (EnumMemberAttribute)field.GetCustomAttribute(typeof(EnumMemberAttribute));
            string value = attribute != null ? attribute.Value : enumValue.ToString();

            Console.WriteLine($"Enum value for {enumValue}: {value}");  // Debug logging
            return value;
        }
    }
}
