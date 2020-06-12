using System;
using libclang;

namespace ClangCaster.Types
{
    public class StructType : UserType
    {
        public StructType(uint hash, string name) : base(hash, name)
        { }

        public static StructType Parse(in CXCursor cursor)
        {
            throw new NotImplementedException();
        }
    }
}
