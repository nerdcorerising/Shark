using System;
using System.Collections.Specialized;
using System.Text;
using Shark;

namespace SharkConsole
{
    public class TestServer : SharkServerBase
    {
        [Path("/hello")]
        public Response Hello()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Hello from the simple test case!");

            builder.AppendLine("Arguments:");
            foreach (string name in Request.QueryString.Keys)
            {
                builder.AppendLine($"    {name}:{Request.QueryString[name]}");
            }

            return builder.ToString();
        }

        [Path("/argtest/{count:int}/{name}")]
        public Response ArgumentTest(int count, string name)
        {
            return $"ArgumentTest: count:{count} name:{name}";
        }
    }
}
