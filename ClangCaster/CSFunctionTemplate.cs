using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        static Dictionary<string, string> s_defaultValueReplaceMap = new Dictionary<string, string>
        {
            {"FLT_MAX", "float.MaxValue"},
        };

        static bool HasDefaultParam(in FunctionParam param)
        {
            if (param.DefaultParamTokens is null)
            {
                return false;
            }

            if (param.HasDefaultOptionalValue)
            {
                return true;
            }

            var values = param.DefaultParamTokens;
            if (values.SequenceEqual(new[] { "ImVec2", "(", "0", ",", "0", ")" }))
            {
                return true;
            }
            if (values.SequenceEqual(new[] { "ImVec4", "(", "0", ",", "0", ",", "0", ",", "0", ")" }))
            {
                return true;
            }

            return false;
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

                // replace
                if (param.DefaultParamTokens != null)
                {
                    for (int i = 0; i < param.DefaultParamTokens.Length; ++i)
                    {
                        if (s_defaultValueReplaceMap.TryGetValue(param.DefaultParamTokens[i], out string replace))
                        {
                            param.DefaultParamTokens[i] = replace;
                        }
                    }
                }

                var defaultValue = "";
                if (HasDefaultParam(param))
                {
                    defaultValue = "default";
                }
                else if (param.DefaultParamTokens != null && param.DefaultParamTokens.Length > 0)
                {
                    defaultValue = string.Join("", param.DefaultParamTokens);
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

        public static bool HasNonDefaultOptionalValue(in FunctionParam param)
        {
            if (param.DefaultParamTokens is null)
            {
                return false;
            }
            if (param.DefaultParamTokens.Length == 0)
            {
                return false;
            }
            if (HasDefaultParam(param))
            {
                return false;
            }

            return true;
        }

        public static IEnumerable<FunctionType> GetOverloads(FunctionType functionType)
        {
            if (functionType.Params.Any(x => HasNonDefaultOptionalValue(x)))
            {
                // 省略無い版

                // 1

                // ... N

                throw new NotImplementedException();
            }
            else
            {
                yield return functionType;
            }
        }

        public string Render(TypeReference reference, string dll)
        {
            var functionType = reference.Type as FunctionType;
            var sb = new StringBuilder();
            foreach (var f in GetOverloads(functionType))
            {
                if (sb.Length != 0)
                {
                    sb.AppendLine();
                }

                // オーバーロードがある場合に複数出力する
                var render = m_template.Render(DotLiquid.Hash.FromAnonymousObject(
                    new
                    {
                        function = new
                        {
                            Hash = reference.Hash,
                            Location = reference.Location,
                            Count = reference.Count,
                            Name = f.Name,
                            Params = f.Params,
                            Return = Converter.Convert(TypeContext.Return, f.Result).Item1,
                        },
                        dll = dll,
                    }
                ));
                sb.Append(render);
            }

            return sb.ToString();
        }
    }
}
