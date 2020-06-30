using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClangAggregator.Types;
using CSType;


namespace ClangCaster
{
    class CSFunctionTemplate
    {
        string EntryPointTemplate => @"        // {{ function.Path }}:{{ function.Line }}
        {{ function.Attribute }}
        public static extern {{ function.Return }} {{function.Name}}(
{% for param in function.Params -%}
            {{ param.Render }}
{%- endfor -%}
        );
";

        DotLiquid.Template m_entrypoint;

        string OverloadTemplate => @"        // overload
        public static {{ function.Return }} {{function.Name}}(
{% for param in function.Params -%}
            {{ param.Render }}
{%- endfor -%}
        )
        {
            {{ function.Body }}
        }
";

        DotLiquid.Template m_overload;

        static Dictionary<string, string> s_defaultValueReplaceMap = new Dictionary<string, string>
        {
            {"FLT_MAX", "float.MaxValue"},
        };

        static string[] s_defaultValue = new string[]
        {
            "NULL", "0", "false", "0.0", "0.0f"
        };

        static bool HasDefaultParam(string[] values)
        {
            if (values is null)
            {
                return false;
            }

            if (values.Length == 1
            && s_defaultValue.Contains(values[0]))
            {
                return true;
            }

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

        static string MakeOptionValue(string[] values)
        {
            var defaultValue = "";
            if (HasDefaultParam(values))
            {
                defaultValue = "default";
            }
            else if (values != null && values.Length > 0)
            {
                defaultValue = string.Join("", values);
            }

            defaultValue = defaultValue.Replace("ImVec2", "new Vector2");
            defaultValue = defaultValue.Replace("ImVec4", "new Vector4");
            defaultValue = defaultValue.Replace("FLT_MAX", "float.MaxValue");

            return defaultValue;
        }

        static (string, string) ParamValue(in FunctionParam param, string defaultValue)
        {
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
            return (csType, csAttr);
        }

        static string ParamCall(in FunctionParam param)
        {
            var (csType, csAttr) = ParamValue(param, "");
            var value = param.Name;
            if (csType.StartsWith("ref "))
            {
                value = "ref " + value;
            }
            return value;
        }

        public CSFunctionTemplate()
        {
            m_entrypoint = DotLiquid.Template.Parse(EntryPointTemplate);
            m_overload = DotLiquid.Template.Parse(OverloadTemplate);

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

                var defaultValue = MakeOptionValue(param.DefaultParamTokens);

                var (csType, csAttr) = ParamValue(param, defaultValue);

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

        /// <summary>
        /// C# の OptionalValue で表現できる値
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool CanCSOptionalValue(string[] values)
        {
            if (values is null || values.Length == 0)
            {
                return true;
            }
            if (HasDefaultParam(values))
            {
                return true;
            }

            {
                var joined = string.Join("", values);
                if (int.TryParse(joined, out _))
                {
                    return true;
                }
                if (float.TryParse(joined, out _))
                {
                    return true;
                }
                if (joined.Last() == 'f')
                {
                    if (float.TryParse(joined.Substring(0, joined.Length - 1), out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public string Render(string path, TypeReference reference, string dll)
        {
            var functionType = reference.Type as FunctionType;

            var sb = new StringBuilder();
            if (functionType.Name == "Image")
            {
                var a = 0;
            }
            var ret = Converter.Convert(TypeContext.Return, functionType.Result).Item1;

            if (functionType.Params.Any(x => !CanCSOptionalValue(x.DefaultParamTokens)))
            {
                // clear
                var copy = new List<FunctionParam>();
                for (int i = 0; i < functionType.Params.Count; ++i)
                {
                    var param = functionType.Params[i];
                    copy.Add(param);
                }

                //
                // entry point
                //
                var found = -1;
                for (int i = 0; i < functionType.Params.Count; ++i)
                {
                    if (!CanCSOptionalValue(copy[i].DefaultParamTokens))
                    {
                        found = i;
                    }
                }
                for (int i = 0; i <= found; ++i)
                {
                    // remove optional value before last !CanCSOptionalValue
                    var param = functionType.Params[i];
                    param.DefaultParamTokens = null;
                    functionType.Params[i] = param;
                }
                {
                    var entryPoint = "";
                    if (!string.IsNullOrEmpty(functionType.MangledName) && functionType.MangledName != functionType.Name)
                    {
                        entryPoint = $", EntryPoint = \"{functionType.MangledName}\"";
                    }
                    var attribute = $"[DllImport(\"{dll}.dll\"{entryPoint})]";
                    var render = m_entrypoint.Render(DotLiquid.Hash.FromAnonymousObject(
                        new
                        {
                            function = new
                            {
                                Attribute = attribute,
                                Path = path,
                                Line = reference.Location.Line,
                                Count = reference.Count,
                                Name = functionType.Name,
                                Params = functionType.Params,
                                Return = ret,
                            },
                        }
                    ));
                    sb.Append(render);
                }

                //
                // overload
                //
                for (int i = copy.Count - 1; i >= 0; --i)
                // for (int i = 0; i < copy.Count; ++i)
                {
                    if (!CanCSOptionalValue(copy[i].DefaultParamTokens))
                    {
                        // setup params
                        functionType.Params.Clear();

                        if (i > 0)
                        {
                            var f = false;
                            for (int j = 0; j < i; ++j)
                            {
                                functionType.Params.Add(default);
                            }
                            for (int j = i - 1; j >= 0; --j)
                            {
                                if (!CanCSOptionalValue(copy[j].DefaultParamTokens))
                                {
                                    f = true;
                                }
                                var param = copy[j];
                                if (f)
                                {
                                    param.DefaultParamTokens = null;
                                }
                                functionType.Params[j] = param;
                            }
                            functionType.Params[functionType.Params.Count - 1] = functionType.Params.Last().MakeLast();
                        }

                        {
                            var args = functionType.Params.Take(i).Select(x => ParamCall(x)).Concat(new string[]{
                                MakeOptionValue(copy[i].DefaultParamTokens)
                            });
                            var body = $"{functionType.Name}({string.Join(", ", args)});";
                            if (ret != "void")
                            {
                                body = "return " + body;
                            }
                            var render = m_overload.Render(DotLiquid.Hash.FromAnonymousObject(
                                new
                                {
                                    function = new
                                    {
                                        Location = reference.Location,
                                        Count = reference.Count,
                                        Name = functionType.Name,
                                        Params = functionType.Params,
                                        Return = ret,
                                        Body = body,
                                    },
                                }
                            ));
                            sb.Append(render);
                        }
                    }
                }
            }
            else
            {
                var entryPoint = "";
                if (!string.IsNullOrEmpty(functionType.MangledName) && functionType.MangledName != functionType.Name)
                {
                    entryPoint = $", EntryPoint = \"{functionType.MangledName}\"";
                }
                var attribute = $"[DllImport(\"{dll}.dll\"{entryPoint})]";
                var render = m_entrypoint.Render(DotLiquid.Hash.FromAnonymousObject(
                    new
                    {
                        function = new
                        {
                            Attribute = attribute,
                            Path = path,
                            Line = reference.Location.Line,
                            Count = reference.Count,
                            Name = functionType.Name,
                            Params = functionType.Params,
                            Return = ret,
                        },
                    }
                ));
                sb.Append(render);
            }

            return sb.ToString();
        }
    }
}
