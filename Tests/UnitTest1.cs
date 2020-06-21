using System;
using System.IO;
using ClangAggregator;
using ClangAggregator.Types;
using CSType;
using Xunit;

namespace Tests
{
    public class EnvFixture : IDisposable
    {
        public EnvFixture()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", $"{path};C:\\Program Files\\LLVM\\bin");
        }

        public void Dispose()
        {
        }
    }

    public class UnitTest1 : IClassFixture<EnvFixture>
    {
        [Fact]
        public void FileInfoTest()
        {
            var a = new FileInfo("C:/a.txt");
            var b = new FileInfo("C:/a.txt");
            Assert.Equal(a, b);
            var c = new FileInfo("C:\\a.txt");
            Assert.Equal(a, c);
        }

        static void ConvertTest(string expected, TypeContext context, BaseType baseType)
        {
            var value = Converter.Convert(context, baseType).Item1;
            Assert.Equal(expected, value);
        }

        [Fact]
        public void CSTypeConvertFieldTest()
        {
            ConvertTest("void", TypeContext.Field, VoidType.Instance);
            ConvertTest("bool", TypeContext.Field, BoolType.Instance);
            ConvertTest("sbyte", TypeContext.Field, Int8Type.Instance);
            ConvertTest("short", TypeContext.Field, Int16Type.Instance);
            ConvertTest("int", TypeContext.Field, Int32Type.Instance);
            ConvertTest("long", TypeContext.Field, Int64Type.Instance);
            ConvertTest("byte", TypeContext.Field, UInt8Type.Instance);
            ConvertTest("ushort", TypeContext.Field, UInt16Type.Instance);
            ConvertTest("uint", TypeContext.Field, UInt32Type.Instance);
            ConvertTest("ulong", TypeContext.Field, UInt64Type.Instance);
            ConvertTest("float", TypeContext.Field, Float32Type.Instance);
            ConvertTest("double", TypeContext.Field, Float64Type.Instance);
            ConvertTest("IntPtr", TypeContext.Field, new PointerType(TypeReference.FromPrimitive(VoidType.Instance)));
            // ConvertTest("IntPtr", TypeContext.Field, new PointerType(new PointerType(VoidType.Instance)));
            ConvertTest("IntPtr", TypeContext.Field, new PointerType(TypeReference.FromPrimitive(Int32Type.Instance)));
        }

        [Fact]
        public void CSTypeConvertParamTest()
        {
            // ConvertTest("ref IntPtr", TypeContext.Param, new PointerType(new PointerType(VoidType.Instance)));
            // ConvertTest("ref int", TypeContext.Field, new PointerType(Int32Type.Instance));
        }

        [Fact]
        public void ParseTest()
        {
            var source = @"
struct Hoge
{
    int Value;
};
";
            var tu = ClangTU.Parse(source);
            var aggregator = new TypeAggregator();
            var map = aggregator.Process(tu.GetCursor());

            var a = 0;
        }
    }
}
