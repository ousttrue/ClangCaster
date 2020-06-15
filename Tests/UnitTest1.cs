using System;
using System.IO;
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
    }
}
