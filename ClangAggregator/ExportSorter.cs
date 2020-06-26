using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClangAggregator.Types;

namespace ClangAggregator
{
    /// <summary>
    /// ソースファイルひとつに属する型情報を仕分けする
    /// 
    /// dllの関数呼び出しが目的なので、
    /// DllExportとみなされた関数を起点に、返り値と引き数に使われている方を再帰的に収集する。
    /// </summary>
    public class ExportSorter
    {
        List<NormalizedFilePath> m_rootHeaders = new List<NormalizedFilePath>();
        List<string> m_dllList = new List<string>();

        Dictionary<NormalizedFilePath, ExportSource> m_headerMap = new Dictionary<NormalizedFilePath, ExportSource>();

        public IDictionary<NormalizedFilePath, ExportSource> HeaderMap => m_headerMap;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers">Exportしたい関数が含まれている header </param>
        public ExportSorter(IEnumerable<HeaderWithDll> headers)
        {
            foreach (var header in headers)
            {
                m_rootHeaders.Add(new NormalizedFilePath(header.Header));
                m_dllList.Add(header.Dll);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var kv in m_headerMap)
            {
                sb.AppendLine(kv.Value.ToString());
            }
            return sb.ToString();
        }

        bool IsContainedInRootHeaders(FileLocation location)
        {
            return m_rootHeaders.Any(x => x.Equals(location.Path));
        }

        bool IsComInterface(TypeReference reference)
        {
            if (reference.Type is StructType structType)
            {
                return structType.IID != default;
            }
            else
            {
                return false;
            }
        }

        string GetDll(NormalizedFilePath path)
        {
            for (int i = 0; i < m_rootHeaders.Count; ++i)
            {
                if (m_rootHeaders[i].Equals(path))
                {
                    return m_dllList[i];
                }
            }

            return default;
        }

        ExportSource GetOrCreateSource(NormalizedFilePath path)
        {
            if (!m_headerMap.TryGetValue(path, out ExportSource export))
            {
                var dll = GetDll(path);
                export = new ExportSource(path, dll);
                m_headerMap.Add(path, export);
            }
            return export;
        }

        public static string[] UseConstantPrefixies = new string[]
        {
            "WS_S_",
            "WS_E_",
            "WS_",
            "MSG_",
            "SW_",
            "CW_",
            "WM_",
            "COLOR_",
            "QS_",
            "PM_",
            "CS_",
            "IDC_",
            "SM_",
        };

        public void PushConstant(ConstantDefinition constant)
        {
            // ensure ExportSource
            if (string.IsNullOrEmpty(constant.Location.Path.Path))
            {
                return;
            }

            // function rename ?
            if (constant.IsRename)
            {
                return;
            }

            // root か prefix が明示されているものだけ
            if (IsContainedInRootHeaders(constant.Location)
            || UseConstantPrefixies.Any(x => constant.Name.StartsWith(x))
            )
            {
                var export = GetOrCreateSource(constant.Location.Path);
                export.PushConstant(constant);
            }
        }

        /// <summary>
        /// root header(コンストラクタ引数) から参照されている型を登録する。
        /// </summary>
        /// <param name="reference"></param>
        public void PushIf(TypeReference reference)
        {
            if (IsContainedInRootHeaders(reference.Location))
            {
                if (reference.Type is FunctionType)
                {
                    Add(reference, new UserType[] { });
                }
                else if (reference.Type is EnumType)
                {
                    Add(reference, new UserType[] { });
                }
                else if (reference.Type is TypedefType)
                {
                    Add(reference, new UserType[] { });
                }
            }
            if (IsComInterface(reference))
            {
                Add(reference, new UserType[] { });
            }
        }

        static bool IsTagName(string name)
        {
            if (name.StartsWith("tag")) return true;
            if (name.StartsWith("_")) return true;
            if (name.EndsWith("_tag")) return true;
            return false;
        }

        /// <summary>
        /// Enum, Struct, Function, Typedef を登録する。
        /// Struct, Function, Typedef から間接的に参照されている型を再帰的に登録する。
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="stack"></param>
        private void Add(TypeReference reference, UserType[] stack)
        {
            if (reference is null)
            {
                return;
            }

            var t = reference.Type;

            // dereference
            if (t is PointerType pointerType)
            {
                Add(pointerType.Pointee, stack);
                return;
            }
            if (t is ArrayType arrayType)
            {
                Add(arrayType.Element, stack);
                return;
            }

            var type = t as UserType;
            if (type is null)
            {
                return;
            }

            if (stack.Contains(type))
            {
                // avoid recursive loop
                return;
            }
            stack = stack.Concat(new[] { type }).ToArray();

            // ensure ExportSource
            if (!string.IsNullOrEmpty(reference.Location.Path.Path))
            {
                var export = GetOrCreateSource(reference.Location.Path);
                if (!export.Push(reference))
                {
                    return;
                }
            }

            // 依存する型を再帰的にAddする
            if (type is EnumType)
            {
                // end
            }
            else if (type is TypedefType typedefType)
            {
                if (typedefType.TryCreatePointerStructType(out StructType pointerStructType))
                {
                    // HWND
                    reference.Type = pointerStructType;
                    Add(reference, stack);
                    return;
                }

                if (typedefType.TryDereference<UserType>(out UserType userType))
                {
                    if (string.IsNullOrEmpty(userType.Name))
                    {
                        // 名無しの宣言に名前を付ける
                        userType.Name = typedefType.Name;
                    }
                    // else if (userType is EnumType)
                    // {
                    //     // enum の tag 名と typedef名を同じにする
                    //     userType.Name = typedefType.Name;
                    // }
                    // else if (userType is StructType)
                    // {
                    //     // struct の tag 名と typedef名を同じにする
                    //     userType.Name = typedefType.Name;
                    // }
                    else if (IsTagName(userType.Name))
                    {
                        // replace tagName
                        userType.Name = typedefType.Name;
                    }
                }

                Add(typedefType.Ref, stack);
                return;
            }
            else if (type is StructType structType)
            {
                foreach (var field in structType.Fields)
                {
                    Add(field.Ref, stack);
                }

                Add(structType.BaseClass, stack);

                foreach (var method in structType.Methods)
                {
                    Add(new TypeReference(default, default, method), stack);
                }

                return;
            }
            else if (type is FunctionType functionType)
            {
                // ret
                Add(functionType.Result, stack);

                // args
                foreach (var param in functionType.Params)
                {
                    Add(param.Ref, stack);
                }

                return;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
