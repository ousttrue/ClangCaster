using System.Collections.Generic;
using ClangAggregator;
using ClangAggregator.Types;

namespace ClangCaster
{
    class ExportHeader
    {
        readonly NormalizedFilePath m_path;

        readonly List<EnumType> m_enumTypes = new List<EnumType>();
        public List<EnumType> EnumTypes => m_enumTypes;

        public ExportHeader(NormalizedFilePath path)
        {
            m_path = path;
        }

        public ExportHeader(string path) : this(new NormalizedFilePath(path))
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
                if (m_enumTypes.Contains(enumType))
                {
                    return;
                }
                m_enumTypes.Add(enumType);
            }
            else
            {
                // TODO:
            }
        }
    }
}
