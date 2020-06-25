using System;
using System.Linq;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    class CSUnionTemplate : CSTemplateBase
    {
        public static string[] Using = new string[]
        {
            "System",
            "System.Runtime.InteropServices",
        };

        protected override string TemplateSource => @"    // {{ type.Location.Path.Path }}:{{ type.Location.Line }}
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public struct {{ type.Name }} // {{ type.Count }}
    {
{% for field in type.Fields -%}        
        [FieldOffset({{ field.Offset }})] {% if field.Attribute %}{{ field.Attribute }} {% endif %}{{ field.Render }}
{%- endfor -%}
    }
";

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
            return m_template.Render(DotLiquid.Hash.FromAnonymousObject(
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
                    },
                }
            ));
        }
    }
}
