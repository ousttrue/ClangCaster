using System;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    class CSStructGenerator : CSUserTypeGeneratorBase
    {
        protected override string TemplateSource => @"using System;
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

        public CSStructGenerator()
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
                var name = CSType.CSSymbole.Escape(field.Name);

                return new
                {
                    Attribute = attribute,
                    Render = $"public {type} {name};",
                };
            };

            DotLiquid.Template.RegisterSafeType(typeof(StructType), new string[] { "Name", "Hash", "Location", "Count", "Fields" });
            DotLiquid.Template.RegisterSafeType(typeof(StructField), FieldFunc);
        }

        public string Render(string ns, StructType structType)
        {
            return m_template.Render(DotLiquid.Hash.FromAnonymousObject(
                new
                {
                    ns = ns,
                    type = structType,
                }
            ));
        }
    }
}
