using System;
using Shark;

namespace SharkConsole
{
    public class TestServer
    {
        [Get("/hello")]
        public string Hello()
        {
            return "Hello from the simple test case!";
        }

        [Get("/argtest")]
        public string ArgumentTest(int count, string name)
        {
            return $"ArgumentTest: count:{count} name:{name}";
        }
    }
}
