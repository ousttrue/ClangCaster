using System;
using libclang;

namespace ClangCaster.Types
{
    public class TypedefType : UserType
    {
        public TypeReference Ref;

        public TypedefType(uint hash, string name) : base(hash, name)
        { }

        public override string ToString()
        {
            return $"typedef {Name} = {Ref.Type}";
        }

        public static TypedefType Parse(in CXCursor cursor)
        {
            var hash = index.clang_hashCursor(cursor);
            var location = ClangLocation.Create(cursor);
            using (var spelling = ClangString.FromCursor(cursor))
            {
                var type = new TypedefType(hash, spelling.ToString());
                return type;
            }
        }
    }
}

