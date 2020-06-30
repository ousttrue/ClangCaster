using System;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    class CSDelegateTemplate : CSTemplateBase
    {
        protected override string TemplateSource => @"    // {{ function.Path }}:{{ function.Line }}
    public delegate {{ function.Return }} {{function.Name}}(
{% for param in function.Params -%}
        {{ param.Render }}
{%- endfor -%}
    );
";

        public CSDelegateTemplate()
        {
            Func<Object, Object> ParamFunc = (Object src) =>
            {
                var param = (FunctionParam)src;
                var comma = param.IsLast ? "" : ",";
                var name = param.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = $"__param__{param.Index}";
                }
                name = CSType.CSSymbole.Escape(name);

                var csType = Converter.Convert(TypeContext.Param, param.Ref).Item1;
                return new
                {
                    Render = $"{csType} {name}{comma}",
                };
            };
            DotLiquid.Template.RegisterSafeType(typeof(FunctionParam), ParamFunc);
        }

        public string Render(string path, TypeReference reference)
        {
            var (name, functionType) = reference.Type.GetFunctionTypeFromTypedef();
            return m_template.Render(DotLiquid.Hash.FromAnonymousObject(
                new
                {
                    function = new
                    {
                        Hash = reference.Hash,
                        Path = path,
                        Line = reference.Location.Line,
                        Count = reference.Count,
                        Name = name,
                        Params = functionType.Params,
                        Return = Converter.Convert(TypeContext.Return, functionType.Result).Item1,
                    }
                }
            ));
        }
    }
}
