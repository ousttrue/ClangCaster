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

        readonly List<EnumType> m_enumTypes = new List<EnumType>();
        public List<EnumType> EnumTypes => m_enumTypes;

        readonly List<StructType> m_structTypes = new List<StructType>();
        public List<StructType> StructTypes => m_structTypes;

        readonly List<FunctionType> m_functionTypes = new List<FunctionType>();
        public List<FunctionType> FunctionTypes => m_functionTypes;

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

        public bool Contains(ClangAggregator.Types.UserType type)
        {
            return m_path.Equals(type.Location.Path);
        }

        public void Push(ClangAggregator.Types.UserType type)
        {
            if (type is EnumType enumType)
            {
                if (m_enumTypes.Any(x => x.Hash == type.Hash))
                {
                    return;
                }
                m_enumTypes.Add(enumType);
            }
            else if (type is StructType structType)
            {
                if (m_structTypes.Any(x => x.Hash == type.Hash))
                {
                    return;
                }
                m_structTypes.Add(structType);
            }
            else if (type is FunctionType functionType)
            {
                if (!functionType.DllExport)
                {
                    return;
                }
                if (m_functionTypes.Any(x => x.Hash == type.Hash))
                {
                    return;
                }
                m_functionTypes.Add(functionType);
            }
            else if (type is TypedefType typedefType)
            {
                // TODO:
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
