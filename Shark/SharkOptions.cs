
using System;
using System.Threading;

namespace Shark
{
    public class SharkOptions
    {
        public string Url { get; set; }
        public int? WorkerThreadCount { get; set; }
        public ErrorHandler Handler { get; set; }
    }
}