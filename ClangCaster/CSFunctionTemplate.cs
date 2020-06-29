using System;
using System.Collections.Generic;
using System.Linq;
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

        static string[] s_defaultValue = new string[]
        {
            "NULL", "0", "false", "0.0f"
        };

        static Dictionary<string, string> s_defaultValueReplaceMap = new Dictionary<string, string>
        {
            {"FLT_MAX", "float.MaxValue"},
        };

        static string DefaultParam(string[] values)
        {
            if (values.Length == 1 && s_defaultValue.Contains(values[0]))
            {
                return "default";
            }
            if (values.SequenceEqual(new[] { "ImVec2", "(", "0", ",", "0", ")" }))
            {
                return "default";
            }
            if (values.SequenceEqual(new[] { "ImVec4", "(", "0", ",", "0", ",", "0", ",", "0", ")" }))
            {
                return "default";
            }

            for (int i = 0; i < values.Length; ++i)
            {
                if (s_defaultValueReplaceMap.TryGetValue(values[i], out string replace))
                {
                    values[i] = replace;
                }
            }

            return string.Join("", values);
        }

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

                var defaultValue = "";
                if (param.DefaultParamTokens != null && param.DefaultParamTokens.Length > 0)
                {
                    defaultValue = DefaultParam(param.DefaultParamTokens);
                }

                var (csType, csAttr) = Converter.Convert(TypeContext.Param, param.Ref);
                if (csType == "ref sbyte" && param.IsConst)
                {
                    // const char *
                    csType = "string";
                    csAttr = "[MarshalAs(UnmanagedType.LPUTF8Str), In]";
                }
                else if (csType.StartsWith("ref ") && param.IsConst)
                {
                    // const &
                    csType = $"in {csType.Substring(4)}";
                }
                else if (csType.StartsWith("ref ") && defaultValue == "default")
                {
                    // null_ptr を渡せるようにとりあえず IntPtr にする。
                    csType = "IntPtr";
                }

                // attribute
                if (!string.IsNullOrEmpty(csAttr))
                {
                    csType = $"{csAttr} {csType}";
                }
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    defaultValue = $" = {defaultValue}";
                }

                return new
                {
                    Render = $"{csType} {name}{defaultValue}{comma}",
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
