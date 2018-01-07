using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace ConsoleApp1
{
    class Program
    {
        public class ImageInfo
        {
            public string Directory { get; set; }
            public string File { get; set; }
            public int Size { get; set; }
            public bool DownloadPermitted { get; set; }
        };

        static void Main(string[] args)
        {
            var v = Resolver.From<ImageInfo>();
            Console.WriteLine("Hello World!");
        }
    }

    static class Resolver
    {
        public static EntityResolver<T> From<T>() where T : new()
        {
            var tType = typeof(T);
            var pk = Expression.Parameter(typeof(string));
            var rk = Expression.Parameter(typeof(string));
            var ts = Expression.Parameter(typeof(DateTimeOffset));
            var props = Expression.Parameter(typeof(IDictionary<string, EntityProperty>));
            var etag = Expression.Parameter(typeof(string));
            var newType = Expression.New(tType);
            var outputVar = Expression.Assign(Expression.Variable(tType), newType);

            var properties = tType.GetProperties().Select(property =>
            {
                var getProp = Expression.Property(props, "Item", Expression.Constant(property.Name));
                var propType = property.PropertyType;
                Expression access = null;

                if (propType == typeof(string))
                {
                    access = Expression.Property(getProp, "StringValue");
                }

                if (propType == typeof(int))
                {
                    access = Expression.Coalesce(Expression.Property(getProp, "Int32Value"), Expression.Constant(-1));
                }

                if (propType == typeof(bool))
                {
                    access = Expression.Coalesce(Expression.Property(getProp, "BooleanValue"), Expression.Constant(false));
                }

                return Expression.Bind(property, access);
            });

            var body = Expression.MemberInit(newType, properties);

            var retv = Expression.Lambda<EntityResolver<T>>(
               body, new List<ParameterExpression> { pk, rk, ts, props, etag }
               );

            return retv.Compile();
        }
    }
}
