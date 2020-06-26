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
            List<HeaderWithDll> headers, string ns, bool dllExportOnly, string constantsClassName = "C")
        {
            // organize types
            var sorter = new ExportSorter(headers);
            foreach (var kv in map)
            {
                sorter.PushIf(kv.Value);
            }
            foreach (var constant in map.Constants)
            {
                sorter.PushConstant(constant);
            }

            // prepare
            int anonymous = 0;
            foreach (var (sourcePath, exportSource) in sorter.HeaderMap)
            {
                foreach (var reference in exportSource.StructTypes)
                {
                    var structType = reference.Type as StructType;
                    if (string.IsNullOrEmpty(structType.Name))
                    {
                        // 無名型に名前を付ける(unionによくある)
                        structType.Name = $"__Global__{anonymous++}";
                    }
                }
            }
            Console.WriteLine($"AnonymousCount: {anonymous}");

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

                //
                // Enum
                //
                if (exportSource.EnumTypes.Any())
                {
                    var enumsDir = new DirectoryInfo(Path.Combine(dir, $"enums"));
                    enumsDir.Create();
                    foreach (var reference in exportSource.EnumTypes)
                    {
                        var enumType = reference.Type as EnumType;
                        if (string.IsNullOrEmpty(enumType.Name))
                        {
                            continue;
                        }
                        using (var s = NamespaceOpener.Open(enumsDir, $"{enumType.Name}.cs", ns))
                        {
                            s.Writer.Write(enumTemplate.Render(reference));
                        }
                    }
                }

                //
                // Struct
                // 
                if (exportSource.StructTypes.Any())
                {
                    var structsDir = new DirectoryInfo(Path.Combine(dir, $"structs"));
                    structsDir.Create();
                    foreach (var reference in exportSource.StructTypes)
                    {
                        var structType = reference.Type as StructType;
                        using (var s = NamespaceOpener.Open(structsDir, $"{structType.Name}.cs", ns, CSStructTemplate.Using))
                        {                           
                            // if (structType.IsUnion)
                            // {
                            //     s.Writer.Write(unionTemplate.Render(reference));
                            // }
                            // else
                            {
                                s.Writer.Write(structTemplate.Render(reference));
                            }
                        }
                    }
                }

                //
                // ComInterface
                //
                if (exportSource.Interfaces.Any())
                {
                    // COM interface
                    var interfacesDir = new DirectoryInfo(Path.Combine(dir, $"interfaces"));
                    interfacesDir.Create();
                    foreach (var reference in exportSource.Interfaces)
                    {
                        var structType = reference.Type as StructType;
                        structType.CalcVTable();
                        using (var s = NamespaceOpener.Open(interfacesDir, $"{structType.Name}.cs", ns, CSComInterfaceTemplate.Using))
                        {
                            s.Writer.Write(interfaceTemplate.Render(reference));
                        }
                    }
                }

                //
                // Functions & Delgates
                //
                if (exportSource.FunctionTypes.Any()
                || exportSource.TypedefTypes.Where(x => x.Type.GetFunctionTypeFromTypedef().Item2 != null).Any())
                {
                    var path = ExportFile(dst, sourcePath);
                    using (var s = new NamespaceOpener(new FileInfo(path), ns, CSFunctionTemplate.Using))
                    {
                        // delegates
                        foreach (var reference in exportSource.TypedefTypes)
                        {
                            if (reference.Type.GetFunctionTypeFromTypedef().Item2 != null)
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
                                    if (dllExportOnly)
                                    {
                                        if (!functionType.DllExport)
                                        {
                                            continue;
                                        }
                                    }
                                    if (functionType.IsDelegate)
                                    {
                                        continue;
                                    }
                                    s.Writer.WriteLine(functionTemplate.Render(reference, exportSource.Dll));
                                }

                                // close partial class
                                s.Writer.WriteLine("    }");
                            }
                        }
                    }
                }

                //
                // Constants
                //
                if (exportSource.Constants.Any())
                {
                    using (var s = NamespaceOpener.Open(new DirectoryInfo(dir), $"constants.cs", ns))
                    {
                        // open static class
                        s.Writer.Write($@"    public static partial class {constantsClassName}
    {{
");
                        foreach (var constant in exportSource.Constants)
                        {
                            if (constant.Name == "IID_ID3DBlob")
                            {
                                var a = 0;
                            }

                            constant.Prepare();

                            // TODO:
                            if (constant.Name == "CINDEX_VERSION_STRING")
                            {
                                continue;
                            }
                            if (constant.Values.Contains("sizeof"))
                            {
                                continue;
                            }
                            if (constant.Values.Any(x => x.Contains('"')))
                            {
                                // non int
                                continue;
                            }

                            s.Writer.WriteLine(CSConstantTemplate.Render(constant));
                        }

                        // close constants
                        s.Writer.WriteLine("    }");
                    }
                }
            }

            // ComPtr
            if (interfaceTemplate.Counter > 0)
            {
                var path = Path.Combine(dst.FullName, "__ComPtr__.cs");
                using (var s = new NamespaceOpener(new FileInfo(path), ns, ComPtr.Using))
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
