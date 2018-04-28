using System;
using System.Collections.Specialized;
using System.Text;
using Shark;

namespace SharkConsole
{
    public class TestServer
    {
        [Get("/hello")]
        public Response Hello()
        {
            return "Hello from the simple test case!";
        }

        [Get("/argtest")]
        public Response ArgumentTest(int count, string name)
        {
            return $"ArgumentTest: count:{count} name:{name}";
        }

        [Get("/namevalue")]
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
