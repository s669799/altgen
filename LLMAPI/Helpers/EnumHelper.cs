using System;
using System.Runtime.Serialization;
using System.Reflection;

namespace LLMAPI.Helpers
{
    public static class EnumHelper
    {
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
