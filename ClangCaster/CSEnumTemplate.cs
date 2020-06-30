using ClangAggregator.Types;

namespace ClangCaster
{
    class CSEnumTemplate : CSTemplateBase
    {
        protected override string TemplateSource => @"    // {{ type.Path }}:{{ type.Line }}
    public enum {{ type.Name }}
    {
{% for value in type.Values -%}
        {{ value.Name }} = {{ value.Hex }},
{%- endfor -%}
    }
";

        public CSEnumTemplate()
        {
            DotLiquid.Template.RegisterSafeType(typeof(EnumValue), new string[] { "Name", "Value", "Hex" });
        }

        public string Render(string path, TypeReference reference)
        {
            var enumType = reference.Type as EnumType;
            return m_template.Render(DotLiquid.Hash.FromAnonymousObject(
                new
                {
                    type = new
                    {
                        Hash = reference.Hash,
                        Path = path,
                        Line = reference.Location.Line,
                        Count = reference.Count,
                        Name = enumType.Name,
                        Values = enumType.Values,
                    },
                }
            ));
        }
    }
}
