using System;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    class CSComInterfaceTemplate : CSTemplateBase
    {
        protected override string TemplateSource => @"    // {{ type.Location.Path.Path }}:{{ type.Location.Line }}
    public class {{ type.Name }} : {{ type.Base }} // {{ type.Count }}
    {
        static Guid s_uuid = new Guid(""{{ type.IID }}"");
        public static new ref Guid IID => ref s_uuid;
        public override ref Guid GetIID() { return ref s_uuid; }

{% for field in type.Fields -%}
        {% if field.Attribute %}{{ field.Attribute }} {% endif %}{{ field.Render }}
{%- endfor -%}
    }
";

        public int Counter { get; private set; }

        public CSComInterfaceTemplate()
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
            Counter += 1;
            var structType = reference.Type as StructType;
            var baseClass = structType.BaseClass != null ? structType.BaseClass.Type.Name : "ComPtr";
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
                        IID = structType.IID.ToString(),
                        Base = baseClass,
                    },
                }
            ));
        }
    }
}
