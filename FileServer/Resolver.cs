using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace FileServer1
{
    public static class Resolver
    {
        private static Dictionary<Type, Expression> Cache { get; } = new Dictionary<Type, Expression>();

        public static EntityResolver<T> From<T>() where T : class, new()
        {
            if (!Cache.TryGetValue(typeof(T), out var rawExpresion))
            {
                rawExpresion = Generator<T>();
            }

            return (rawExpresion as Expression<EntityResolver<T>>).Compile();
        }

        private static Expression Generator<T>() where T : new()
        {
            Expression<EntityResolver<T>> template =
                (string pk, string rk, DateTimeOffset ts, IDictionary<string, EntityProperty> props, string etag) => default(T);

            var rowProps = template.Parameters[3];

            var containsKey = typeof(IDictionary<string, EntityProperty>).GetMethod("ContainsKey");

            var properties = typeof(T).GetProperties().Select(outProperty =>
            {
                var propName = Expression.Constant(outProperty.Name);

                // Use the "Item" accessor to dereference the dictionary here
                var item = Expression.Property(rowProps, "Item", propName);

                // Set the value only if the row has it
                var contains = Expression.Call(rowProps, containsKey, propName);
                var accessor = GetAccessor(item, outProperty.PropertyType);
                var defaultValue = Expression.Default(outProperty.PropertyType);
                var test = Expression.Condition(contains, accessor, defaultValue);

                return Expression.Bind(outProperty, test);
            });

            var body = Expression.MemberInit(Expression.New(typeof(T)), properties);

            var resolver = Expression.Lambda<EntityResolver<T>>(
               body,
               $"{typeof(T).Name}Resolver",
               false,
               template.Parameters
               );

            Cache[typeof(T)] = resolver;

            return resolver;
        }

        private static Expression GetAccessor(Expression item, Type type)
        {
            Expression accessor = null;

            if (type == typeof(string))
            {
                return Expression.Property(item, "StringValue");
            }

            Dictionary<Type, string> knownTypes = new Dictionary<Type, string>
            {
                [typeof(int)] = "Int32Value",
                [typeof(bool)] = "BooleanValue",
                [typeof(DateTime)] = "DateTime",
                [typeof(DateTimeOffset)] = "DateTimeOffsetValue",
                [typeof(Double)] = "DoubleValue",
                [typeof(Guid)] = "GuidValue",
            };

            if (knownTypes.TryGetValue(type, out var accessorName))
            {
                accessor = Expression.Coalesce(Expression.Property(item, accessorName), Expression.Default(type));
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlying = Nullable.GetUnderlyingType(type);

                if (knownTypes.TryGetValue(underlying, out accessorName))
                {
                    accessor = Expression.Property(item, accessorName);
                }
            }

            if (accessor == null)
            {
                throw new Exception($"GetAccessor didn't recognize the type '{type.Name}' - contact author for fix");
            }

            return accessor;
        }

        [Serializable]
        private class Exception : System.Exception
        {
            public Exception()
            {
            }

            public Exception(string message) : base(message)
            {
            }

            public Exception(string message, System.Exception innerException) : base(message, innerException)
            {
            }

            protected Exception(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
