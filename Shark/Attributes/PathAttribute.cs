
using System;
using System.Collections.Generic;

namespace Shark
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PathAttribute : Attribute
    {
        public PathAttribute(string path, string[] methods = null)
        {
            Path = path;
            Methods = methods ?? new string[] { "get" };
        }

        public string Path { get; private set; }
        public IEnumerable<string> Methods { get; private set; }
    }
}
