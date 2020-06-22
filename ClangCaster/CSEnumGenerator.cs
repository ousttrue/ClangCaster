using ClangAggregator.Types;

namespace ClangCaster
{
    class CSEnumGenerator : CSUserTypeGeneratorBase
    {
        protected override string TemplateSource => @"    // {{ type.Location.Path.Path }}:{{ type.Location.Line }}
    public enum {{ type.Name }} // {{ type.Count }}
    {
{% for value in type.Values -%}
        {{ value.Name }} = {{ value.Hex }},
{%- endfor -%}
    }
";

        public CSEnumGenerator()
        {
            DotLiquid.Template.RegisterSafeType(typeof(EnumValue), new string[] { "Name", "Value", "Hex" });
        }

        public string Render(TypeReference reference)
        {
            var enumType = reference.Type as EnumType;
            return m_template.Render(DotLiquid.Hash.FromAnonymousObject(
                new
                {
                    type = new
                    {
                        Hash = reference.Hash,
                        Location = reference.Location,
                        Count = reference.Count,
                        Name = enumType.Name,
                        Values = enumType.Values,
                    },
                }
            ));
        }
    }
}
