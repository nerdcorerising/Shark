using System;
namespace Shark
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DeleteAttribute : Attribute
    {
        public DeleteAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }
    }
}
