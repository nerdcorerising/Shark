using System;
using Xunit;

namespace SharkTests
{
    public class BasicTests
    {
        // TODO:
        // empty route
        // just /
        // nested path route
        // wildcard route
        // arguments of each type I expect
        // user defined argument throws exception
        // argument name case insensitivity
        // arguments order doesn't matter
        // more arguments than the method has fails
        // Name value pair arguments work with arbitary arguments
        [Fact]
        public void Test1()
        {
            Assert.True(1 == 1);
        }
    }
}
