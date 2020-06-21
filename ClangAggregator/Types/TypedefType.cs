using CIndex;

namespace ClangAggregator.Types
{
    public class TypedefType : UserType
    {
        public TypeReference Ref;

        TypedefType(string name) : base(name)
        { }

        public override string ToString()
        {
            return $"typedef {Name} = {Ref.Type}";
        }

        public static TypedefType Parse(in CXCursor cursor, TypeMap typeMap)
        {
            var type = new TypedefType(cursor.Spelling());
            var underlying = libclang.clang_getTypedefDeclUnderlyingType(cursor);
            type.Ref = typeMap.CxTypeToType(underlying, cursor);
            return type;
        }
    }
}
