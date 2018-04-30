
using System;
using System.Net;

namespace Shark
{
    public abstract class SharkServerBase
    {
        internal protected HttpListenerRequest Request { get; set; }
    }
}
