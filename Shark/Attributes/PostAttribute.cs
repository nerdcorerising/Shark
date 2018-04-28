using System;
namespace Shark
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PostAttribute : Attribute
    {
        public PostAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }
    }
}
