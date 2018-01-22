using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Utilities;

namespace ConsoleApp1
{
    class Program
    {
        public class ImageInfo
        {
            public string Directory { get; set; }
            public string File { get; set; }
            public int Size { get; set; }
            public int DownloadCount { get; set; }
            public bool DownloadPermitted { get; set; }

            public BoolHolder Test { get; set; }

            // BUGBUG remove
            public class BoolHolder
            {
                public bool BoolValue { get; set; }

                public static void RegisterBoolHolder()
                {
                    Resolver.AddConverter(s => new BoolHolder
                    {
                        BoolValue = bool.Parse(s)
                    });
                }
            }
        };

        static void Main(string[] args)
        {
            var s = new List<int> { 1, 2, 3, 4, };
            var t = new List<int>(s) { 5, };

            ImageInfo.BoolHolder.RegisterBoolHolder();
            Resolver.TestPropertyExists = false;
            Resolver.WrapExceptions = false;
            var v = Resolver.From<ImageInfo>();

            Console.WriteLine("Hello World!");

            ImageInfo imageInfo = new ImageInfo();

        }
    }

}
