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

    public static class TypedefExtensions
    {
        /// <summary>
        /// HWND など
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool TryCreatePointerStructType(this TypedefType type, out StructType dst)
        {
            if (type.Ref.Type is PointerType pointerType)
            {
                if (pointerType.Pointee.Type is StructType structType)
                {
                    // HWND__
                    if (structType.Name.EndsWith("__"))
                    {
                        dst = StructType.CreatePointerStructType(type.Name);
                        return true;
                    }
                }
            }

            dst = default;
            return false;
        }
    }
}
