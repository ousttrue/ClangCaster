using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClangAggregator;
using ClangAggregator.Types;

namespace ClangCaster
{
    abstract class CSTemplateBase
    {
        protected DotLiquid.Template m_template;

        abstract protected string TemplateSource { get; }

        protected CSTemplateBase()
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

        public void Export(
            TypeMap map,
            DirectoryInfo dst,
            List<HeaderWithDll> headers, string ns)
        {
            // organize types
            var sorter = new ExportSorter(headers);
            foreach (var kv in map)
            {
                sorter.PushIfRootFunction(kv.Value);
            }
            foreach (var constant in map.Constants)
            {
                foreach (var prefix in CSConstantTemplate.UseConstantPrefixies)
                {
                    if (constant.Name.StartsWith(prefix))
                    {
                        sorter.PushConstant(prefix, constant);
                        break;
                    }
                }
            }

            // export
            DotLiquid.Template.RegisterSafeType(typeof(TypeReference), new string[] { "Type" });
            DotLiquid.Template.RegisterSafeType(typeof(FileLocation), new string[] { "Path", "Line" });
            DotLiquid.Template.RegisterSafeType(typeof(NormalizedFilePath), new string[] { "Path" });

            var enumTemplate = new CSEnumTemplate();
            var structTemplate = new CSStructTemplate();
            var interfaceTemplate = new CSComInterfaceTemplate();
            var delegateTemplate = new CSDelegateTemplate();
            var functionTemplate = new CSFunctionTemplate();
            foreach (var (sourcePath, exportSource) in sorter.HeaderMap)
            {
                Console.WriteLine($"export: {sourcePath}");
                var dir = ExportDir(dst, sourcePath);

                if (exportSource.EnumTypes.Any())
                {
                    var enumsDir = new DirectoryInfo(Path.Combine(dir, $"enums"));
                    enumsDir.Create();
                    foreach (var reference in exportSource.EnumTypes)
                    {
                        var enumType = reference.Type as EnumType;
                        using (var s = NamespaceOpener.Open(enumsDir, $"{enumType.Name}.cs", ns))
                        {
                            s.Writer.Write(enumTemplate.Render(reference));
                        }
                    }
                }

                if (exportSource.StructTypes.Any())
                {
                    var structsDir = new DirectoryInfo(Path.Combine(dir, $"structs"));
                    structsDir.Create();
                    foreach (var reference in exportSource.StructTypes)
                    {
                        var structType = reference.Type as StructType;
                        using (var s = NamespaceOpener.Open(structsDir, $"{structType.Name}.cs", ns))
                        {
                            s.Writer.Write(structTemplate.Render(reference));
                        }
                    }
                }

                if (exportSource.Interfaces.Any())
                {
                    // COM interface
                    var interfacesDir = new DirectoryInfo(Path.Combine(dir, $"interfaces"));
                    interfacesDir.Create();
                    foreach (var reference in exportSource.Interfaces)
                    {
                        var structType = reference.Type as StructType;
                        using (var s = NamespaceOpener.Open(interfacesDir, $"{structType.Name}.cs", ns))
                        {
                            s.Writer.Write(interfaceTemplate.Render(reference));
                        }
                    }
                }

                if (exportSource.FunctionTypes.Any()
                || exportSource.TypedefTypes.Where(x => x.GetFunctionTypeFromTypedef().Item2 != null).Any())
                {
                    var path = ExportFile(dst, sourcePath);
                    using (var s = new NamespaceOpener(new FileInfo(path), ns, "System", "System.Runtime.InteropServices"))
                    {
                        // delegates
                        foreach (var reference in exportSource.TypedefTypes)
                        {
                            if (reference.GetFunctionTypeFromTypedef().Item2 != null)
                            {
                                s.Writer.WriteLine(delegateTemplate.Render(reference));
                            }
                        }

                        if (exportSource.FunctionTypes.Any())
                        {
                            if (string.IsNullOrEmpty(exportSource.Dll))
                            {
                                Console.WriteLine("dll name not specified. please use -h PATH_TO_HEADER.h,NAME.dll");
                            }
                            else
                            {
                                // open partial class
                                s.Writer.Write($@"    public static partial class {exportSource.Dll}
    {{
");
                                // functions
                                foreach (var reference in exportSource.FunctionTypes)
                                {
                                    var functionType = reference.Type as FunctionType;
                                    s.Writer.WriteLine(functionTemplate.Render(reference, exportSource.Dll));
                                }

                                // close partial class
                                s.Writer.WriteLine("    }");
                            }
                        }
                    }
                }

                if (exportSource.ConstantMap.Any())
                {
                    var constantsDir = new DirectoryInfo(Path.Combine(dir, $"constants"));
                    constantsDir.Create();
                    foreach (var (prefix, list) in exportSource.ConstantMap)
                    {
                        using (var s = NamespaceOpener.Open(constantsDir, $"{prefix}.cs", ns))
                        {
                            // open static class
                            s.Writer.Write($@"    public static partial class {prefix}
    {{
");

                            foreach (var constant in list)
                            {
                                constant.Prepare(prefix);
                                s.Writer.WriteLine(CSConstantTemplate.Render(constant));
                            }

                            // close constants
                            s.Writer.WriteLine("    }");
                        }
                    }
                }
            }

            // ComPtr
            if (interfaceTemplate.Counter > 0)
            {
                var path = Path.Combine(dst.FullName, "__ComPtr__.cs");
                using (var s = new NamespaceOpener(new FileInfo(path), ns))
                {
                    s.Writer.Write(ComPtr.Source);
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
