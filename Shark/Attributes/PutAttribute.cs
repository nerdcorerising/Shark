using System;
namespace Shark
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PutAttribute : Attribute
    {
        public PutAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }
    }
}
