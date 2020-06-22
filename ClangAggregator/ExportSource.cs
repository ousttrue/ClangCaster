using System;
using System.Collections.Generic;
using System.Linq;
using ClangAggregator.Types;

namespace ClangAggregator
{
    /// <summary>
    /// ひとつのソースに属するエクスポート対象の型を保持する
    /// </summary>
    public class ExportSource
    {
        readonly NormalizedFilePath m_path;

        readonly List<TypeReference> m_enumTypes = new List<TypeReference>();
        public IEnumerable<EnumType> EnumTypes => m_enumTypes.Select(x => x.Type as EnumType);

        readonly List<TypeReference> m_structTypes = new List<TypeReference>();
        public IEnumerable<StructType> StructTypes => m_structTypes.Select(x => x.Type as StructType);

        readonly List<TypeReference> m_functionTypes = new List<TypeReference>();
        public IEnumerable<FunctionType> FunctionTypes => m_functionTypes.Select(x => x.Type as FunctionType);

        public bool IsEmpty => EnumTypes.Any() || StructTypes.Any() || FunctionTypes.Any();

        public ExportSource(NormalizedFilePath path)
        {
            m_path = path;
        }

        public ExportSource(string path) : this(new NormalizedFilePath(path))
        {
        }

        public override string ToString()
        {
            return $"{m_path} ({m_enumTypes.Count}types)";
        }

        public bool Contains(ClangAggregator.Types.TypeReference reference)
        {
            return m_path.Equals(reference.Location.Path);
        }

        int m_anonymous;

        public void Push(ClangAggregator.Types.TypeReference reference)
        {
            var type = reference.Type;
            if (type is EnumType enumType)
            {
                if (m_enumTypes.Any(x => x.Hash == reference.Hash))
                {
                    return;
                }

                // enum値名の重複する部分を除去する
                enumType.PreparePrefix();

                m_enumTypes.Add(reference);
            }
            else if (type is StructType structType)
            {
                if (m_structTypes.Any(x => x.Hash == reference.Hash))
                {
                    return;
                }

                if (string.IsNullOrEmpty(structType.Name))
                {
                    // 無名型に名前を付ける(unionによくある)
                    structType.Name = $"__Anonymous__{m_anonymous++}";
                }

                m_structTypes.Add(reference);
            }
            else if (type is FunctionType functionType)
            {
                if (!functionType.DllExport)
                {
                    return;
                }
                if (m_functionTypes.Any(x => x.Hash == reference.Hash))
                {
                    return;
                }
                m_functionTypes.Add(reference);
            }
            else if (type is TypedefType typedefType)
            {
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
