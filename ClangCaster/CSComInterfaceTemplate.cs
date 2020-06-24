using System;
using System.Linq;
using ClangAggregator.Types;
using CSType;

namespace ClangCaster
{
    class CSComInterfaceTemplate : CSTemplateBase
    {
        public static string[] Using = new string[]
        {
            "System",
            "System.Runtime.InteropServices",
        };

        protected override string TemplateSource => @"    // {{ type.Location.Path.Path }}:{{ type.Location.Line }}
    public class {{ type.Name }} : {{ type.Base }} // {{ type.Count }}
    {
        static Guid s_uuid = new Guid(""{{ type.IID }}"");
        public static new ref Guid IID => ref s_uuid;
        public override ref Guid GetIID() { return ref s_uuid; }

{% for method in type.Methods -%}
        public {{ method.Return }} {{method.Name}}({{method.ParamsWithName}})
        {
            var fp = GetFunctionPointer({{method.VTableIndex}});
            if(m_{{ method.Name }}Func==null) m_{{ method.Name }}Func = ({{ method.Name }}Func)Marshal.GetDelegateForFunctionPointer(fp, typeof({{ Method.Name }}Func));
            
            {{ method.ReturnNorVoid }} m_{{ method.Name }}Func(m_ptr{{method.Comma}}{{method.Call}});
        }
        delegate {{ method.Return }} {{ method.Name }}Func(IntPtr self{{method.Comma}}{{method.ParamsWithName}});
        {{ method.Name }}Func m_{{ method.Name }}Func;

{%- endfor -%}
    }
";

        public int Counter { get; private set; }

        static string Call(in FunctionParam param)
        {
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

            if (csType.StartsWith("ref "))
            {
                name = $"ref {name}";
            }

            return name;
        }

        static string ParamsWithName(in FunctionParam param)
        {
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

            return $"{csType} {name}";
        }

        public CSComInterfaceTemplate()
        {
            Func<Object, Object> MethodFunc = (Object src) =>
            {
                var functionType = (FunctionType)src;
                var paramsWithName = string.Join(", ", functionType.Params.Select(x => ParamsWithName(x)).ToArray());
                var call = String.Join(", ", functionType.Params.Select(x => Call(x)).ToArray());
                var returnType = Converter.Convert(TypeContext.Return, functionType.Result).Item1;
                return new
                {
                    Name = functionType.Name,
                    Params = functionType.Params,
                    Return = returnType,
                    VTableIndex = functionType.VTableIndex,
                    ParamsWithName = paramsWithName,
                    Call = call,
                    Comma = functionType.Params.Count == 0 ? "" : ", ",
                    ReturnNorVoid = returnType == "void" ? "" : "return "
                };
            };

            DotLiquid.Template.RegisterSafeType(typeof(FunctionType), MethodFunc);
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
                        Methods = structType.Methods,
                    },
                }
            ));
        }
    }
}
