using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileServer
{
    public class DeviceQueryAttribute : Attribute
    {
        public string Field { get; set; }
        public Func<string, string> Converter { get; set; }

        private static Dictionary<Type, string> SelectStatement { get; } = new Dictionary<Type, string>();

        public DeviceQueryAttribute(string field)
        {
            Field = field;
        }

        public static string CreateSelect<T>(string whereClause = null)
        {
            if(SelectStatement.TryGetValue(typeof(T), out var select))
            {
                return $"{select} {GetWhereClause(whereClause)}";
            }

            var properties = typeof(T).GetProperties().Select(prop => new
            {
                Property = prop,
                Attribute = prop.GetCustomAttributes(typeof(DeviceQueryAttribute), false).FirstOrDefault()
            })
            .Where(pa => pa.Attribute != null)
            .Select(pa => $"{(pa.Attribute as DeviceQueryAttribute).Field} as {pa.Property.Name}");

            SelectStatement[typeof(T)] = $"select {string.Join(", ", properties)} from devices";

            return CreateSelect<T>(whereClause);
        }

        private static string GetWhereClause(string whereClause)
        {
            if (string.IsNullOrWhiteSpace(whereClause))
            {
                whereClause = string.Empty;
            }
            else
            {
                whereClause = whereClause.Trim();
                if (!whereClause.StartsWith("where ", StringComparison.InvariantCultureIgnoreCase))
                {
                    whereClause = "where " + whereClause;
                }
            }

            return whereClause;
        }
    }
}
