using System;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    class CSFunctionTemplate : CSTemplateBase
    {
        protected override string TemplateSource => @"        // {{ function.Location.Path.Path }}:{{ function.Location.Line }}
        [DllImport(""{{ dll }}.dll"")]
        public static extern {{ function.Return }} {{function.Name}}(
{% for param in function.Params -%}
            {{ param.Render }}
{%- endfor -%}
        );
";

        public CSFunctionTemplate()
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

                var (csType, csAttr) = Converter.Convert(TypeContext.Param, param.Ref);
                if (!string.IsNullOrEmpty(csAttr))
                {
                    csType = $"{csAttr} {csType}";
                }
                return new
                {
                    Render = $"{csType} {name}{comma}",
                };
            };
            DotLiquid.Template.RegisterSafeType(typeof(FunctionParam), ParamFunc);
        }

        public string Render(TypeReference reference, string dll)
        {
            var functionType = reference.Type as FunctionType;
            return m_template.Render(DotLiquid.Hash.FromAnonymousObject(
                new
                {
                    function = new
                    {
                        Hash = reference.Hash,
                        Location = reference.Location,
                        Count = reference.Count,
                        Name = functionType.Name,
                        Params = functionType.Params,
                        Return = Converter.Convert(TypeContext.Return, functionType.Result).Item1,
                    },
                    dll = dll,
                }
            ));
        }
    }
}
