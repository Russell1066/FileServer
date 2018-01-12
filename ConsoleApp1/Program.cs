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
            public int? Test { get; set; }
            public bool DownloadPermitted { get; set; }
        };

        static void Main(string[] args)
        {
            var v = Resolver.From<ImageInfo>();
            Console.WriteLine("Hello World!");

            ImageInfo imageInfo = new ImageInfo();
            
        }
    }

}
