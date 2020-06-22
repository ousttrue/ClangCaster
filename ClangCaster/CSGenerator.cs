using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClangAggregator;
using ClangAggregator.Types;

namespace ClangCaster
{
    abstract class CSUserTypeGeneratorBase
    {
        protected DotLiquid.Template m_template;

        abstract protected string TemplateSource { get; }

        protected CSUserTypeGeneratorBase()
        {
            m_template = DotLiquid.Template.Parse(TemplateSource);
        }
    }

    /// <summary>
    /// CSharpのコードを生成する
    /// </summary>
    class CSGenerator
    {
        static string ExportFile(DirectoryInfo directory, NormalizedFilePath f)
        {
            var stem = Path.GetFileNameWithoutExtension(f.Path);
            return Path.Combine(Path.Combine(directory.FullName, $"{stem}.cs"));
        }


        static string ExportDir(DirectoryInfo directory, NormalizedFilePath f)
        {
            var stem = Path.GetFileNameWithoutExtension(f.Path);
            return Path.Combine(Path.Combine(directory.FullName, stem));
        }

        public void Export(IDictionary<NormalizedFilePath, ExportSource> map, DirectoryInfo dst, string ns, string dll)
        {
            DotLiquid.Template.RegisterSafeType(typeof(TypeReference), new string[] { "Type" });
            DotLiquid.Template.RegisterSafeType(typeof(FileLocation), new string[] { "Path", "Line" });
            DotLiquid.Template.RegisterSafeType(typeof(NormalizedFilePath), new string[] { "Path" });

            var enumTemplate = new CSEnumGenerator();
            var structTemplate = new CSStructGenerator();
            var functionTemplate = new CSFunctionGenerator();
            foreach (var (sourcePath, exportSource) in map)
            {
                Console.WriteLine(sourcePath);
                var dir = ExportDir(dst, sourcePath);

                if (exportSource.EnumTypes.Any())
                {
                    var enumsDir = new DirectoryInfo(Path.Combine(dir, $"enums"));
                    enumsDir.Create();
                    foreach (var enumType in exportSource.EnumTypes)
                    {
                        using (var s = NamespaceOpener.Open(enumsDir, $"{enumType.Name}.cs", ns))
                        {
                            s.Writer.Write(enumTemplate.Render(enumType));
                        }
                    }
                }

                if (exportSource.StructTypes.Any())
                {
                    var structsDir = new DirectoryInfo(Path.Combine(dir, $"structs"));
                    structsDir.Create();
                    foreach (var structType in exportSource.StructTypes)
                    {
                        using (var s = NamespaceOpener.Open(structsDir, $"{structType.Name}.cs", ns))
                        {
                            s.Writer.Write(structTemplate.Render(structType));
                        }
                    }
                }

                if (exportSource.FunctionTypes.Any())
                {
                    var path = ExportFile(dst, sourcePath);
                    using (var s = new NamespaceOpener(new FileInfo(path), ns))
                    {
                        // open partial class
                        s.Writer.Write($@"    public static partial class {dll}
    {{
");

                        foreach (var functionType in exportSource.FunctionTypes)
                        {
                            s.Writer.Write(functionTemplate.Render(dll, functionType));
                        }

                        // close partial class
                        s.Writer.WriteLine("    }");
                    }
                }
            }

            // csproj
            {
                var csproj = Path.Combine(dst.FullName, $"{dst.Name}.csproj");
                using (var s = new FileStream(csproj, FileMode.Create))
                using (var w = new StreamWriter(s))
                {
                    w.WriteLine(@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

</Project>");
                }
            }
        }
    }
}
