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
            type.Ref = typeMap.CxTypeToType(underlying, cursor).Item1;
            return type;
        }
    }

    public static class TypedefExtensions
    {
        /// <summary>
        /// HWND, HMODULE, HBITMAP etc...
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

        public static bool TryDereference<T>(this TypedefType typedef, out T value) where T : BaseType
        {
            if (typedef.Ref.Type is T)
            {
                value = typedef.Ref.Type as T;
                return true;
            }

            if(typedef.Ref.Type is PointerType pointerType)
            {
                if(pointerType.Pointee.Type is T)
                {
                    value = pointerType.Pointee.Type as T;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
