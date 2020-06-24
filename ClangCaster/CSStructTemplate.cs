using System;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    class CSStructTemplate : CSTemplateBase
    {
        public static string[] Using = new string[]
        {
            "System",
            "System.Runtime.InteropServices",
        };

        protected override string TemplateSource => @"    // {{ type.Location.Path.Path }}:{{ type.Location.Line }}
    [StructLayout(LayoutKind.Sequential)]
    public struct {{ type.Name }} // {{ type.Count }}
    {
{% for field in type.Fields -%}
        {% if field.Attribute %}{{ field.Attribute }} {% endif %}{{ field.Render }}
{%- endfor -%}
    }
";

        public CSStructTemplate()
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

                return new
                {
                    Attribute = attribute,
                    Render = $"public {type} {name};",
                };
            };

            DotLiquid.Template.RegisterSafeType(typeof(StructField), FieldFunc);
        }

        public string Render(TypeReference reference)
        {
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
                        Fields = structType.Fields,
                    },
                }
            ));
        }
    }
}
