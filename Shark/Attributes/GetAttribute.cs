﻿using System;
namespace Shark
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class GetAttribute : Attribute
    {
        public GetAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }
    }
}
