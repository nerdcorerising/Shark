
using System;
using System.Threading;

namespace Shark
{
    public class SharkOptions
    {
        public string Url { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}