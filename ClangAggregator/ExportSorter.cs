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

        public void Push(ClangAggregator.Types.UserType type)
        {
            foreach (var root in m_rootHeaders)
            {
                if (root.Equals(type.Location.Path))
                {
                    Add(type, new UserType[] { });
                    return;
                }
            }
            // skip
        }

        void Add(ClangAggregator.Types.UserType type, ClangAggregator.Types.UserType[] stack)
        {
            if (type is null)
            {
                return;
            }
            if (stack.Contains(type))
            {
                // avoid recursive loop
                return;
            }

            // Add
            if (!m_headerMap.TryGetValue(type.Location.Path, out ExportSource export))
            {
                export = new ExportSource(type.Location.Path);
                m_headerMap.Add(type.Location.Path, export);
            }

            if (string.IsNullOrEmpty(type.Name))
            {
                // 名無し。stack を辿って typedef があればその名前をいただく
                if (stack.Any() && stack.Last() is TypedefType stackTypedef)
                {
                    type.Name = stackTypedef.Name;
                }
            }
            export.Push(type);

            // 依存する型を再帰的にAddする
            if (type is EnumType)
            {
                // end
            }
            else if (type is TypedefType typedefType)
            {
                Add(typedefType.Ref.Type as UserType, stack.Concat(new[] { type }).ToArray());
            }
            else if (type is StructType structType)
            {
                foreach (var field in structType.Fields)
                {
                    if (field.Ref.Type is UserType userType)
                    {
                        Add(userType, stack.Concat(new[] { type }).ToArray());
                    }
                }
            }
            else if (type is FunctionType functionType)
            {
                // ret
                {
                    if (functionType.Result.Type is UserType userType)
                    {
                        Add(functionType.Result.Type as UserType, stack.Concat(new[] { type }).ToArray());
                    }
                }

                // args
                foreach (var param in functionType.Params)
                {
                    if (param.Ref.Type is UserType userType)
                    {
                        Add(userType, stack.Concat(new[] { type }).ToArray());
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
