using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClangAggregator;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    /// <summary>
    /// CSharpのコードを生成する
    /// </summary>
    class CSGenerator
    {
        const string COMMNET = "// This source code was generated by ClangCaster";

        const string ENUM_TEMPLATE = @"using System;
using System.Runtime.InteropServices;

namespace {{ ns }} {
    public enum {{ type.Name }} // {{ type.Count }}
    {
{% for value in type.Values -%}
        {{ value.Name }} = {{ value.Hex }},
{%- endfor -%}
    }
}
";

        const string BEGIN = @"using System;
using System.Runtime.InteropServices;

namespace {{ ns }}
{
    public static partial class {{ dll }}
    {
";

        const string END = @"   }
}
";

        const string STRUCT_TEMPLATE = @"using System;
using System.Runtime.InteropServices;

namespace {{ ns }} {
    // {{ type.Location.Path.Path }}:{{ type.Location.Line }}
    [StructLayout(LayoutKind.Sequential)]
    public struct {{ type.Name }} // {{ type.Count }}
    {
{% for field in type.Fields -%}
        {% if field.Attribute %}{{ field.Attribute }} {% endif %}{{ field.Render }}
{%- endfor -%}
    }
}
";

        const string FUNCTION_TEMPLATE = @"
        [DllImport(""{{ dll }}.dll"")]
        public static extern {{ function.Return }} {{function.Name}}(
{% for param in function.Params -%}
            {{ param.Render }}
{%- endfor -%}
        );
";

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

        static string[] CSSymbols = new string[]
        {
            "base",
            "string",
            "event",
        };

        static string EscapeSymbol(string src)
        {
            if (!CSSymbols.Contains(src))
            {
                return src;
            }
            return $"_{src}";
        }

        static bool GetPrimitive(BaseType type, out PrimitiveType primitive)
        {
            if (type is PrimitiveType)
            {
                primitive = type as PrimitiveType;
                return true;
            }

            if (type is TypedefType typedefType)
            {
                return GetPrimitive(typedefType.Ref.Type, out primitive);
            }

            primitive = null;
            return false;
        }

        public void Export(IDictionary<NormalizedFilePath, ExportSource> map, DirectoryInfo dst, string ns, string dll)
        {
            Func<Object, Object> FieldFunc = (Object src) =>
            {
                var field = (StructField)src;

                var (type, attribute) = Converter.Convert(TypeContext.Field, field.Ref.Type);
                if (string.IsNullOrEmpty(type))
                {
                    // anonymous union
                    // throw new NotImplementedException();
                }

                // name
                var name = EscapeSymbol(field.Name);

                return new
                {
                    Attribute = attribute,
                    Render = $"public {type} {name};",
                };
            };

            Func<Object, Object> FunctionFunc = (Object src) =>
            {
                var function = (FunctionType)src;

                var csType = Converter.Convert(TypeContext.Return, function.Result.Type).Item1;

                return new
                {
                    Return = csType,
                    Name = function.Name,
                    Params = function.Params,
                };
            };

            Func<Object, Object> ParamFunc = (Object src) =>
            {
                var param = (FunctionParam)src;
                var comma = param.IsLast ? "" : ",";
                var name = param.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = $"__param__{param.Index + 1}";
                }
                name = EscapeSymbol(name);

                var csType = Converter.Convert(TypeContext.Param, param.Ref.Type).Item1;
                return new
                {
                    Render = $"{csType} {name}{comma}",
                };
            };

            DotLiquid.Template.RegisterSafeType(typeof(StructType), new string[] { "Name", "Hash", "Location", "Count", "Fields" });
            DotLiquid.Template.RegisterSafeType(typeof(StructField), FieldFunc);
            DotLiquid.Template.RegisterSafeType(typeof(FunctionType), FunctionFunc);
            DotLiquid.Template.RegisterSafeType(typeof(FunctionParam), ParamFunc);
            DotLiquid.Template.RegisterSafeType(typeof(TypeReference), new string[] { "Type" });
            DotLiquid.Template.RegisterSafeType(typeof(EnumType), new string[] { "Name", "Hash", "Location", "Count", "Values" });
            DotLiquid.Template.RegisterSafeType(typeof(EnumValue), new string[] { "Name", "Value", "Hex" });
            DotLiquid.Template.RegisterSafeType(typeof(FileLocation), new string[] { "Path", "Line" });
            DotLiquid.Template.RegisterSafeType(typeof(NormalizedFilePath), new string[] { "Path" });

            var enumTemplate = DotLiquid.Template.Parse(ENUM_TEMPLATE);
            var structTemplate = DotLiquid.Template.Parse(STRUCT_TEMPLATE);
            var functionTemplate = DotLiquid.Template.Parse(FUNCTION_TEMPLATE);
            var beginTemplate = DotLiquid.Template.Parse(BEGIN);
            var endTemplate = DotLiquid.Template.Parse(END);
            foreach (var (sourcePath, exportSource) in map)
            {
                Console.WriteLine(sourcePath);
                var dir = ExportDir(dst, sourcePath);

                // enum
                {
                    var enumsDir = new DirectoryInfo(Path.Combine(dir, $"enums"));
                    enumsDir.Create();
                    foreach (var enumType in exportSource.EnumTypes)
                    {
                        enumType.PreparePrefix();
                        var path = new FileInfo(Path.Combine(enumsDir.FullName, $"{enumType.Name}.cs"));
                        using (var s = new FileStream(path.FullName, FileMode.Create))
                        using (var w = new StreamWriter(s))
                        {
                            w.WriteLine(COMMNET);

                            var rendered = enumTemplate.Render(DotLiquid.Hash.FromAnonymousObject(
                                new
                                {
                                    ns = ns,
                                    type = enumType,
                                }
                            ));
                            w.Write(rendered);
                        }
                    }
                }

                // struct
                if (exportSource.StructTypes.Any())
                {
                    var structsDir = new DirectoryInfo(Path.Combine(dir, $"structs"));
                    structsDir.Create();

                    // anonymous types
                    {
                        int i = 0;
                        foreach (var structType in exportSource.StructTypes)
                        {
                            if (string.IsNullOrEmpty(structType.Name))
                            {
                                structType.Name = $"__Anonymous__{i++}";
                            }
                        }
                    }

                    foreach (var structType in exportSource.StructTypes)
                    {
                        var path = new FileInfo(Path.Combine(structsDir.FullName, $"{structType.Name}.cs"));
                        using (var s = new FileStream(path.FullName, FileMode.Create))
                        using (var w = new StreamWriter(s))
                        {
                            w.WriteLine(COMMNET);

                            var rendered = structTemplate.Render(DotLiquid.Hash.FromAnonymousObject(
                                new
                                {
                                    ns = ns,
                                    type = structType,
                                }
                            ));
                            w.Write(rendered);
                        }
                    }
                }

                if (exportSource.FunctionTypes.Any())
                {
                    var path = ExportFile(dst, sourcePath);
                    using (var s = new FileStream(path, FileMode.Create))
                    using (var w = new StreamWriter(s))
                    {
                        w.WriteLine(COMMNET);

                        // BEGIN
                        w.Write(beginTemplate.Render(DotLiquid.Hash.FromAnonymousObject(
                            new
                            {
                                ns = ns,
                                dll = dll,
                            }
                        )));

                        // function
                        foreach (var functionType in exportSource.FunctionTypes)
                        {
                            var rendered = functionTemplate.Render(DotLiquid.Hash.FromAnonymousObject(
                                new
                                {
                                    function = functionType,
                                    dll = dll,
                                }
                            ));
                            w.Write(rendered);
                        }

                        // END
                        w.Write(endTemplate.Render(DotLiquid.Hash.FromAnonymousObject(
                            new
                            {
                            }
                        )));
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
