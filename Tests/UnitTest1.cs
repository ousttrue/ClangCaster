using System;
using System.IO;
using ClangAggregator.Types;
using CSType;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var a = new FileInfo("C:/a.txt");
            var b = new FileInfo("C:/a.txt");
            Assert.Equal(a, b);
            var c = new FileInfo("C:\\a.txt");
            Assert.Equal(a, c);
        }

        [Fact]
        public void CSTypeTest()
        {
            Assert.Equal("int", Converter.Convert(TypeContext.Field, Int32Type.Instance).Item1);
        }
    }
}
