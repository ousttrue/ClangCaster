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

        bool IsRootFunction(TypeReference reference)
        {
            if (!m_rootHeaders.Any(x => x.Equals(reference.Location.Path)))
            {
                return false;
            }

            return reference.Type is FunctionType;
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

        public void PushConstant(string prefix, ConstantDefinition constant)
        {
            // ensure ExportSource
            if (string.IsNullOrEmpty(constant.Location.Path.Path))
            {
                return;
            }

            var export = GetOrCreateSource(constant.Location.Path);
            export.PushConstant(prefix, constant);
        }

        /// <summary>
        /// root header(コンストラクタ引数) から参照されている FunctionType を登録する。
        /// </summary>
        /// <param name="reference"></param>
        public void PushIfRootFunction(TypeReference reference)
        {
            if (IsRootFunction(reference))
            {
                Add(reference, new UserType[] { });
            }
        }

        /// <summary>
        /// Enum, Struct, Function, Typedef を登録する。
        /// Struct, Function, Typedef から間接的に参照されている型を再帰的に登録する。
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="stack"></param>
        private void Add(TypeReference reference, ClangAggregator.Types.UserType[] stack)
        {
            var t = reference.Type;
            if (t is PointerType pointerType)
            {
                // pointer
                Add(pointerType.Pointee, stack);
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

            // ensure ExportSource
            if (string.IsNullOrEmpty(reference.Location.Path.Path))
            {
                return;
            }
            var export = GetOrCreateSource(reference.Location.Path);

            export.Push(reference);

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

                if (typedefType.Ref.Type is UserType userType)
                {
                    if (string.IsNullOrEmpty(userType.Name))
                    {
                        // 名無しの宣言に名前を付ける
                        userType.Name = typedefType.Name;
                    }
                    else if (userType is EnumType)
                    {
                        // enum の tag 名と typedef名を同じにする
                        userType.Name = typedefType.Name;
                    }
                    else if (userType is StructType)
                    {
                        // struct の tag 名と typedef名を同じにする
                        userType.Name = typedefType.Name;
                    }
                }

                Add(typedefType.Ref, stack.Concat(new[] { type }).ToArray());
            }
            else if (type is StructType structType)
            {
                foreach (var field in structType.Fields)
                {
                    Add(field.Ref, stack.Concat(new[] { type }).ToArray());
                }
            }
            else if (type is FunctionType functionType)
            {
                // ret
                Add(functionType.Result, stack.Concat(new[] { type }).ToArray());

                // args
                foreach (var param in functionType.Params)
                {
                    Add(param.Ref, stack.Concat(new[] { type }).ToArray());
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
