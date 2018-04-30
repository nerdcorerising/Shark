using System;
using System.Collections.Generic;
using Shark;
using Xunit;

namespace SharkTests
{
    public class SharkPathTests
    {
        [Fact]
        public void SimpleRoute()
        {
            SharkPath path = new SharkPath("/hello");
            Assert.True(path.MatchesRoute("/hello"));
            Assert.False(path.MatchesRoute("/"));
            Assert.False(path.MatchesRoute("/hell"));
            Assert.False(path.MatchesRoute("/helloo"));
            Assert.False(path.MatchesRoute("/helio"));
        }

        [Fact]
        public void Empty()
        {
            SharkPath path = new SharkPath("");
            Assert.True(path.MatchesRoute("/"));
            Assert.False(path.MatchesRoute(""));
        }

        [Fact]
        public void DefaultTypeIsString()
        {
            SharkPath path = new SharkPath("/path/{var}/otherpath");
            Dictionary<string, Type> variables = path.GetVariableTypes();
            Assert.True(variables.ContainsKey("var"));
            Assert.Equal(variables["var"], typeof(string));
        }

        [Fact]
        public void CheckVariableValues()
        {
            SharkPath path = new SharkPath("/root/{str:string}/{i:int}");
            Dictionary<string, object> values = path.ParseRoute("/root/astring/1234");
            Assert.Equal(values.Count, 2);

            Assert.True(values.ContainsKey("str"));
            Assert.Equal(values["str"].GetType(), typeof(string));
            Assert.Equal((string)values["str"], "astring");

            Assert.True(values.ContainsKey("i"));
            Assert.Equal(values["i"].GetType(), typeof(int));
            Assert.Equal((int)values["i"], 1234);
        }

        [Fact]
        public void UnsupportedType()
        {
            Assert.Throws<ArgumentException>(() => new SharkPath("/apath/{var:UserType}"));
        }

        [Fact]
        public void EverySupportedType()
        {
            SharkPath path = new SharkPath("/{vs:string}/{vc:char}/{vb:bool}/{vby:byte}/{vsh:short}/{vi:int}/{vl:long}/{vf:float}/{vd:double}/{vus:ushort}/{vui:uint}/{vul:ulong}");
            Dictionary<string, Type> variables = path.GetVariableTypes();
            Assert.True(variables.ContainsKey("vs"));
            Assert.Equal(variables["vs"], typeof(string));

            Assert.True(variables.ContainsKey("vc"));
            Assert.Equal(variables["vc"], typeof(char));

            Assert.True(variables.ContainsKey("vb"));
            Assert.Equal(variables["vb"], typeof(bool));

            Assert.True(variables.ContainsKey("vby"));
            Assert.Equal(variables["vby"], typeof(byte));

            Assert.True(variables.ContainsKey("vsh"));
            Assert.Equal(variables["vsh"], typeof(short));

            Assert.True(variables.ContainsKey("vi"));
            Assert.Equal(variables["vi"], typeof(int));

            Assert.True(variables.ContainsKey("vl"));
            Assert.Equal(variables["vl"], typeof(long));

            Assert.True(variables.ContainsKey("vf"));
            Assert.Equal(variables["vf"], typeof(float));

            Assert.True(variables.ContainsKey("vd"));
            Assert.Equal(variables["vd"], typeof(double));

            Assert.True(variables.ContainsKey("vus"));
            Assert.Equal(variables["vus"], typeof(ushort));

            Assert.True(variables.ContainsKey("vui"));
            Assert.Equal(variables["vui"], typeof(uint));

            Assert.True(variables.ContainsKey("vul"));
            Assert.Equal(variables["vul"], typeof(ulong));

        }

        [Fact]
        public void DuplicateVariables()
        {
            SharkPath path = new SharkPath("/{var1:int}/{var1:int}");
            Assert.Throws<ArgumentException>(() => path.ParseRoute("/1/2"));
        }

        [Fact]
        public void WrongScopeVariables()
        {
            Assert.Throws<ArgumentException>(() => new SharkPath("/apath/{variable1}{variable2}"));
            Assert.Throws<ArgumentException>(() => new SharkPath("/apath/{variable1}/asf{variable2}"));
            Assert.Throws<ArgumentException>(() => new SharkPath("/apath/{variable1}ldd/{variable2}"));
            Assert.Throws<ArgumentException>(() => new SharkPath("/apath/{variable1}agg}"));
        }
    }
}
