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
        List<NormalizedFilePath> m_rootHeaders;
        Dictionary<NormalizedFilePath, ExportSource> m_headerMap = new Dictionary<NormalizedFilePath, ExportSource>();

        public IDictionary<NormalizedFilePath, ExportSource> HeaderMap => m_headerMap;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers">Exportしたい関数が含まれている header </param>
        public ExportSorter(IEnumerable<string> headers)
        {
            m_rootHeaders = headers.Select(x => new NormalizedFilePath(x)).ToList();
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
            if (!m_headerMap.TryGetValue(reference.Location.Path, out ExportSource export))
            {
                export = new ExportSource(reference.Location.Path);
                m_headerMap.Add(reference.Location.Path, export);
            }

            export.Push(reference);

            // 依存する型を再帰的にAddする
            if (type is EnumType)
            {
                // end
            }
            else if (type is TypedefType typedefType)
            {
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

                // TODO: pointer に対する typedef を struct 化する. ex: HWND

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
