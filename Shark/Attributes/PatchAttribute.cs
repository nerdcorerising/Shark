using System;
namespace Shark
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PatchAttribute : Attribute
    {
        public PatchAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }
    }
}
