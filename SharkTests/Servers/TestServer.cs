using System;
using System.Collections.Specialized;
using System.Text;
using Shark;

namespace SharkTests.Servers
{
    public class TestServer
    {
        [Path("/hello")]
        public Response Hello()
        {
            return "Hello from the simple test case!";
        }

        [Path("/argtest")]
        public Response ArgumentTest(int count, string name)
        {
            return $"ArgumentTest: count:{count} name:{name}";
        }

        [Path("/namevalue")]
        public Response NameValueTest(NameValueCollection query)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string key in query.AllKeys)
            {
                builder.AppendLine($"{key}:{query[key]}");
            }

            return builder.ToString();
        }
    }
}
