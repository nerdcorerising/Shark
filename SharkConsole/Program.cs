﻿using System;
using Shark;

namespace SharkConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            server.RunServer<TestServer>();
        }
    }
}
