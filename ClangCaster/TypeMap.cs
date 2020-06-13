using System.Collections;
using System.Collections.Generic;
using ClangCaster.Types;
using libclang;

namespace ClangCaster
{
    /// <summary>
    /// libclangで解析した型を管理する
    /// </summary>
    public class TypeMap : IEnumerable<KeyValuePair<uint, UserType>>
    {
        Dictionary<uint, UserType> m_typeMap = new Dictionary<uint, UserType>();

        public UserType Get(CXCursor cursor)
        {
            var hash = index.clang_hashCursor(cursor);
            if (!m_typeMap.TryGetValue(hash, out UserType type))
            {
                return null;
            }
            return type;
        }

        public void Add(UserType type)
        {
            m_typeMap.Add(type.Hash, type);
        }

        public IEnumerator<KeyValuePair<uint, UserType>> GetEnumerator()
        {
            return m_typeMap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
