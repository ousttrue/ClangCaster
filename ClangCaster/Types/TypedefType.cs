using libclang;

namespace ClangCaster.Types
{
    public class TypedefType : UserType
    {
        public TypeReference Ref;

        TypedefType((uint, ClangLocation, string) args) : base(args)
        { }

        public override string ToString()
        {
            return $"typedef {Name} = {Ref.Type}";
        }

        public static TypedefType Parse(in CXCursor cursor, TypeMap typeMap)
        {
            var type = new TypedefType(cursor.CursorHashLocationSpelling());
            var underlying = index.clang_getTypedefDeclUnderlyingType(cursor);
            type.Ref = typeMap.CxTypeToType(underlying, cursor);
            return type;
        }
    }
}
