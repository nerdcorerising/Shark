using System;
namespace Shark
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class GetAttribute : Attribute
    {
        public GetAttribute(string route)
        {
            Route = route;
        }

        public string Route { get; private set; }
    }
}
