using System;
using Shark;
using Xunit;

namespace SharkTests
{
    public class BasicTests
    {
        class MismatchedParamsTest
        {
            [Path("/{var1:int}/{str}")]
            public Response Method(uint var1, byte str)
            {
                return "";
            }
        }

        [Fact]
        public void MismatchedParams()
        {
            Assert.Throws<ArgumentException>(() => new Server<MismatchedParamsTest>()); 
        }

        class MissingParamsTest
        {
            [Path("/{var1:int}/{str}")]
            public Response Method()
            {
                return "";
            }
        }

        [Fact]
        public void MissingParams()
        {
            Assert.Throws<ArgumentException>(() => new Server<MissingParamsTest>());
        }

        class WrongHttpMethodClass
        {
            [Path("/", methods: new string[] { "stuff" })]
            public Response Method()
            {
                return "";
            }
        }

        [Fact]
        public void WrongHttpMethod()
        {
            Assert.Throws<ArgumentException>(() => new Server<WrongHttpMethodClass>());
        }

        class NoResponseClass
        {
            [Path("/")]
            public void Method()
            {
                
            }
        }

        [Fact]
        public void NoResponse()
        {
            Assert.Throws<ArgumentException>(() => new Server<NoResponseClass>());
        }
    }
}
