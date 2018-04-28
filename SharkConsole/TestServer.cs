using System;
using System.Collections.Specialized;
using System.Text;
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

        [Get("/namevalue")]
        public string NameValueTest(NameValueCollection query)
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
