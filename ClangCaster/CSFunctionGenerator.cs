using System;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    class CSFunctionGenerator : CSUserTypeGeneratorBase
    {
        protected override string TemplateSource => @"
        [DllImport(""{{ dll }}.dll"")]
        public static extern {{ function.Return }} {{function.Name}}(
{% for param in function.Params -%}
            {{ param.Render }}
{%- endfor -%}
        );
";

        public CSFunctionGenerator()
        {
            Func<Object, Object> FunctionFunc = (Object src) =>
            {
                var function = (FunctionType)src;

                var csType = Converter.Convert(TypeContext.Return, function.Result.Type).Item1;

                return new
                {
                    Return = csType,
                    Name = function.Name,
                    Params = function.Params,
                };
            };
            DotLiquid.Template.RegisterSafeType(typeof(FunctionType), FunctionFunc);

            Func<Object, Object> ParamFunc = (Object src) =>
            {
                var param = (FunctionParam)src;
                var comma = param.IsLast ? "" : ",";
                var name = param.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = $"__param__{param.Index + 1}";
                }
                name = CSType.CSSymbole.Escape(name);

                var csType = Converter.Convert(TypeContext.Param, param.Ref.Type).Item1;
                return new
                {
                    Render = $"{csType} {name}{comma}",
                };
            };
            DotLiquid.Template.RegisterSafeType(typeof(FunctionParam), ParamFunc);
        }

        public string Render(string dll, FunctionType functionType)
        {
            return m_template.Render(DotLiquid.Hash.FromAnonymousObject(
                new
                {
                    function = functionType,
                    dll = dll,
                }
            ));
        }
    }
}
