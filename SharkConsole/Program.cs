using System;
using Shark;

namespace SharkConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Server<TestServer> server = new Server<TestServer>();
            server.Run();
        }
    }
}
