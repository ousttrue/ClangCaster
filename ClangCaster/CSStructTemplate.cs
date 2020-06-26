using System;
using System.Linq;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    class CSStructTemplate
    {
        public static string[] Using = new string[]
        {
            "System",
            "System.Runtime.InteropServices",
        };

        protected string StructTemplate => @"    // {{ type.Location.Path.Path }}:{{ type.Location.Line }}
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct {{ type.Name }}
    {
{% for anonymous in type.AnonymousTypes -%}
{{ anonymous.Render }}
{%- endfor -%}   
{% for field in type.Fields -%}
        {% if field.Attribute %}{{ field.Attribute }} {% endif %}{{ field.Render }}
{%- endfor -%}
    }
";

        DotLiquid.Template m_struct;

        protected string UnionTemplate => @"    // {{ type.Location.Path.Path }}:{{ type.Location.Line }}
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public struct {{ type.Name }}
    {
{% for anonymous in type.AnonymousTypes -%}
{{ anonymous.Render }}
{%- endfor -%}   
{% for field in type.Fields -%}        
        [FieldOffset({{ field.Offset }})] {% if field.Attribute %}{{ field.Attribute }} {% endif %}{{ field.Render }}
{%- endfor -%}
    }
";

        DotLiquid.Template m_union;

        public CSStructTemplate()
        {
            m_struct = DotLiquid.Template.Parse(StructTemplate);
            m_union = DotLiquid.Template.Parse(UnionTemplate);
        }

        static string Indent(string src, string indent)
        {
            var splitted = src.Split("\n").Select(x => indent + x);
            return string.Join("\n", splitted);
        }

        public string Render(TypeReference reference)
        {
            Func<Object, Object> FieldFunc = (Object src) =>
            {
                var field = (StructField)src;

                var (type, attribute) = Converter.Convert(TypeContext.Field, field.Ref);
                if (string.IsNullOrEmpty(type))
                {
                    // anonymous union
                    // throw new NotImplementedException();
                }

                // name
                var name = CSType.CSSymbole.Escape(field.Name);
                if (string.IsNullOrEmpty(name))
                {
                    name = $"__field__{field.Index}";
                }

                return new
                {
                    Attribute = attribute,
                    Offset = field.Offset,
                    Render = $"public {type} {name};",
                };
            };

            var structType = reference.Type as StructType;
            if (structType.AnonymousTypes != null)
            {
                for (int i = 0; i < structType.AnonymousTypes.Count; ++i)
                {
                    structType.AnonymousTypes[i].Type.Name = $"__Anonymous__{i}";
                }
            }

            var anonymousTypes = structType.AnonymousTypes != null
                ? structType.AnonymousTypes.Select(x =>
                {
                    var render = Render(x);
                    return new
                    {
                        Render = Indent(render, "    "),
                    };
                }).ToArray()
     : null
                ;

            var template = structType.IsUnion ? m_union : m_struct;

            return template.Render(DotLiquid.Hash.FromAnonymousObject(
                new
                {
                    type = new
                    {
                        Hash = reference.Hash,
                        Location = reference.Location,
                        Count = reference.Count,
                        Name = structType.Name,
                        Fields = structType.Fields.Select(x =>
                        {
                            return FieldFunc(x);
                        }).ToArray(),
                        AnonymousTypes = anonymousTypes,
                    },
                }
            ));
        }
    }
}
