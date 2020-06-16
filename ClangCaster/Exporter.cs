using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClangAggregator;
using ClangAggregator.Types;

namespace ClangCaster
{
    class Exporter
    {
        List<NormalizedFilePath> m_rootHeaders;
        Dictionary<NormalizedFilePath, ExportSource> m_headerMap = new Dictionary<NormalizedFilePath, ExportSource>();

        public IDictionary<NormalizedFilePath, ExportSource> HeaderMap => m_headerMap;

        public Exporter(IEnumerable<string> headers)
        {
            m_rootHeaders = headers.Select(x => new NormalizedFilePath(x)).ToList();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var kv in m_headerMap)
            {
                sb.AppendLine(kv.Value.ToString());
            }
            return sb.ToString();
        }

        public void Push(ClangAggregator.Types.UserType type)
        {
            foreach (var root in m_rootHeaders)
            {
                if (root.Equals(type.Location.Path))
                {
                    Add(type, new UserType[] { });
                    return;
                }
            }
            // skip
        }

        void Add(ClangAggregator.Types.UserType type, ClangAggregator.Types.UserType[] stack)
        {
            if (type is null)
            {
                return;
            }
            if (stack.Contains(type))
            {
                // avoid recursive loop
                return;
            }

            // Add
            if (!m_headerMap.TryGetValue(type.Location.Path, out ExportSource export))
            {
                export = new ExportSource(type.Location.Path);
                m_headerMap.Add(type.Location.Path, export);
            }

            if (string.IsNullOrEmpty(type.Name))
            {
                // 名無し。stack を辿って typedef があればその名前をいただく
                if (stack.Any() && stack.Last() is TypedefType stackTypedef)
                {
                    type.Name = stackTypedef.Name;
                }
            }
            export.Push(type);

            // 依存する型を再帰的にAddする
            if (type is EnumType)
            {
                // end
            }
            else if (type is TypedefType typedefType)
            {
                Add(typedefType.Ref.Type as UserType, stack.Concat(new[] { type }).ToArray());
            }
            else if (type is StructType structType)
            {
                foreach (var field in structType.Fields)
                {
                    if (field.Ref.Type is UserType userType)
                    {
                        Add(userType, stack.Concat(new[] { type }).ToArray());
                    }
                }
            }
            else if (type is FunctionType functionType)
            {
                // ret
                {
                    if (functionType.Result.Type is UserType userType)
                    {
                        Add(functionType.Result.Type as UserType, stack.Concat(new[] { type }).ToArray());
                    }
                }

                // args
                foreach (var param in functionType.Params)
                {
                    if (param.Ref.Type is UserType userType)
                    {
                        Add(userType, stack.Concat(new[] { type }).ToArray());
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        const string ENUM_TEMPLATE = @"// This source code was generated by regenerator""
using System;
using System.Runtime.InteropServices;

namespace {{ ns }}
{
    public enum {{ type.Name }} // {{ type.Count }}
    {
{% for value in type.Values -%}
        {{ value.Name }} = {{ value.Hex }},
{%- endfor -%}
    }
}
";

        const string BEGIN = @"// This source code was generated by regenerator""
using System;
using System.Runtime.InteropServices;

namespace {{ ns }}
{
";

        const string END = @"}
";

        const string STRUCT_TEMPLATE = @"
{% for type in types -%}
    [StructLayout(LayoutKind.Sequential)]    
    public struct {{ type.Name }} // {{ type.Count }}
    {
{% for field in type.Fields -%}
        {% if field.Attribute %}{{ field.Attribute }} {% endif %}{{ field.Render }}
{%- endfor -%}
    }
{%- endfor -%}
";

        const string FUNCTION_TEMPLATE = @"
    public static class {{ name }}
    {
{% for function in functions -%}
        [DllImport(""{{ dll }}"")]
        public static extern {{ function.Return }} {{function.Name}}(
{% for param in function.Params -%}
            {{ param.Render }}
{%- endfor -%}
        );
{%- endfor -%}
    }
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

        static string[] EscapeSymbols = new string[]
        {
            "base",
        };

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

        /// <summary>
        /// ClangCaster.Types.BaseType から CSharp の型を表す文字列と属性(struct用)を返す
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static (string, string) ToCSType(BaseType type)
        {
            if (type is PrimitiveType primitiveType)
            {
                switch (primitiveType)
                {
                    case Int8Type int8Type: return ("sbyte", null);
                    case Int16Type int16Type: return ("short", null);
                    case Int32Type int32Type: return ("int", null);
                    case Int64Type int64Type: return ("long", null);
                    case UInt8Type uint8Type: return ("byte", null);
                    case UInt16Type uint16Type: return ("ushort", null);
                    case UInt32Type uint32Type: return ("uint", null);
                    case UInt64Type uint64Type: return ("ulong", null);
                    case Float32Type float32Type: return ("float", null);
                    case Float64Type float64Type: return ("double", null);
                    case VoidType voidType: return ("void", null);
                }

                throw new NotImplementedException();
            }
            if (type is EnumType enumType)
            {
                return (enumType.Name, null);
            }
            if (type is StructType structType)
            {
                return (structType.Name, null);
            }

            if (type is PointerType pointerType)
            {
                return ("IntPtr", null);
            }

            if (type is ArrayType arrayType)
            {
                var elementType = ToCSType(arrayType.Element.Type).Item1;
                return ($"{elementType}[]", $"[MarshalAs(UnmanagedType.ByValArray, SizeConst = {arrayType.Size})]");
            }

            if (type is TypedefType typedefType)
            {
                return ToCSType(typedefType.Ref.Type);
            }

            throw new NotImplementedException();
        }

        public void Export(DirectoryInfo dst, string ns)
        {
            Func<Object, Object> FieldFunc = (Object src) =>
            {
                var field = (StructField)src;

                var (type, attribute) = ToCSType(field.Ref.Type);

                // name
                var name = field.Name;
                if (EscapeSymbols.Contains(field.Name))
                {
                    // name = $"@{name}";
                    name = $"_{name}";
                }

                return new
                {
                    Attribute = attribute,
                    Render = $"public {type} {name};",
                };
            };
            Func<Object, Object> FunctionFunc = (Object src) =>
            {
                var function = (FunctionType)src;

                var csType = ToCSType(function.Result.Type).Item1;

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
                var csType = ToCSType(param.Ref.Type).Item1;
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
            foreach (var (sourcePath, exportSource) in HeaderMap)
            {
                Console.WriteLine(sourcePath);
                foreach (var enumType in exportSource.EnumTypes)
                {
                    enumType.PreparePrefix();

                    var dir = ExportDir(dst, sourcePath);
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, $"{enumType.Name}.cs");
                    using (var s = new FileStream(path, FileMode.Create))
                    using (var w = new StreamWriter(s))
                    {
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

                {
                    var path = ExportFile(dst, sourcePath);
                    using (var s = new FileStream(path, FileMode.Create))
                    using (var w = new StreamWriter(s))
                    {
                        // BEGIN
                        w.Write(beginTemplate.Render(DotLiquid.Hash.FromAnonymousObject(
                            new
                            {
                                ns = ns,
                            }
                        )));

                        // struct
                        if (exportSource.StructTypes.Any())
                        {
                            var rendered = structTemplate.Render(DotLiquid.Hash.FromAnonymousObject(
                                new
                                {
                                    types = exportSource.StructTypes.Where(x => x.Fields.Any()),
                                }
                            ));
                            w.Write(rendered);
                        }

                        // function
                        if (exportSource.FunctionTypes.Any())
                        {
                            var rendered = functionTemplate.Render(DotLiquid.Hash.FromAnonymousObject(
                                new
                                {
                                    functions = exportSource.FunctionTypes,
                                    name = Path.GetFileNameWithoutExtension(sourcePath.Path),
                                    dll = ns + ".dll",
                                }
                            ));
                            w.Write(rendered);
                        }

                        // END
                        w.Write(endTemplate.Render(DotLiquid.Hash.FromAnonymousObject(
                            new
                            {
                                ns = ns,
                            }
                        )));
                    }
                }
            }
        }
    }
}
