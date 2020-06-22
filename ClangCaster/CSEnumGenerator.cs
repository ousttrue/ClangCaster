using ClangAggregator.Types;

namespace ClangCaster
{
    class CSEnumGenerator : CSUserTypeGeneratorBase
    {
        protected override string TemplateSource => @"using System;
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

        public CSEnumGenerator()
        {
            DotLiquid.Template.RegisterSafeType(typeof(EnumType), new string[] { "Name", "Hash", "Location", "Count", "Values" });
            DotLiquid.Template.RegisterSafeType(typeof(EnumValue), new string[] { "Name", "Value", "Hex" });
        }

        public string Render(string ns, EnumType enumType)
        {
            return m_template.Render(DotLiquid.Hash.FromAnonymousObject(
                new
                {
                    ns = ns,
                    type = enumType,
                }
            ));
        }
    }
}
