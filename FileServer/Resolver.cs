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

        public static EntityResolver<T> From<T>() where T : new()
        {
            if (!Cache.TryGetValue(typeof(T), out var rawExpresion))
            {
                rawExpresion = Generator<T>();
            }

            return (rawExpresion as Expression<EntityResolver<T>>).Compile();
        }

        private static Expression Generator<T>() where T : new()
        {
            var tType = typeof(T);
            var pk = Expression.Parameter(typeof(string));
            var rk = Expression.Parameter(typeof(string));
            var ts = Expression.Parameter(typeof(DateTimeOffset));
            var props = Expression.Parameter(typeof(IDictionary<string, EntityProperty>));
            var etag = Expression.Parameter(typeof(string));
            var newType = Expression.New(tType);
            var outputVar = Expression.Assign(Expression.Variable(tType), newType);

            var properties = tType.GetProperties().Select(outProperty =>
            {
                    // Use the "Item" accessor to dereference the dictionary here
                    var item = Expression.Property(props, "Item", Expression.Constant(outProperty.Name));

                var accessor = GetAccessor(item, outProperty.PropertyType);

                return Expression.Bind(outProperty, accessor);
            });

            var body = Expression.MemberInit(newType, properties);

            var retv = Expression.Lambda<EntityResolver<T>>(
               body, new List<ParameterExpression> { pk, rk, ts, props, etag }
               );

            Cache[tType] = retv;

            return retv;
        }

        private static Expression GetAccessor(Expression item, Type type)
        {
            Expression accessor = null;

            if (type == typeof(string))
            {
                accessor = Expression.Property(item, "StringValue");
            }

            if (type == typeof(int))
            {
                accessor = Expression.Coalesce(Expression.Property(item, "Int32Value"), Expression.Constant(-1));
            }

            if (type == typeof(bool))
            {
                accessor = Expression.Coalesce(Expression.Property(item, "BooleanValue"), Expression.Constant(false));
            }

            if (type == typeof(DateTime))
            {
                accessor = Expression.Coalesce(Expression.Property(item, "DateTime"), Expression.Constant(DateTime.MinValue));
            }

            if (type == typeof(DateTimeOffset))
            {
                accessor = Expression.Coalesce(Expression.Property(item, "DateTimeOffsetValue"), Expression.Constant(DateTimeOffset.MinValue));
            }

            if (type == typeof(Double))
            {
                accessor = Expression.Coalesce(Expression.Property(item, "DoubleValue"), Expression.Constant(Double.MinValue));
            }

            if (type == typeof(Guid))
            {
                accessor = Expression.Coalesce(Expression.Property(item, "GuidValue"), Expression.Constant(Guid.Empty));
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
