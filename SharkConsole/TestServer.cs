using System;
using System.Collections.Specialized;
using System.Text;
using Shark;

namespace SharkConsole
{
    public class TestServer
    {
        [Path("/hello")]
        public Response Hello()
        {
            return "Hello from the simple test case!";
        }

        [Path("/argtest/{count:int}/{name}")]
        public Response ArgumentTest(int count, string name)
        {
            return $"ArgumentTest: count:{count} name:{name}";
        }

        //[Path("/pathtest/{p:path}")]
        //public Response PathTest(string p)
        //{
        //    return $"PathTest: p:{p}";
        //}
    }
}
