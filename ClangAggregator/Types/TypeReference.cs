namespace ClangAggregator.Types
{
    public class TypeReference
    {
        public BaseType Type { get; set; }

        public uint Hash { get; private set; }

        public FileLocation Location;

        public uint Count { get; set; }

        public TypeReference(uint hash, FileLocation location, BaseType type)
        {
            Hash = hash;
            Location = location;
            Type = type;
        }

        public TypeReference((uint, FileLocation) args, BaseType type) : this(args.Item1, args.Item2, type)
        {
        }

        private TypeReference()
        {

        }

        public static TypeReference FromPrimitive(PrimitiveType type)
        {
            return new TypeReference
            {
                Type = type
            };
        }
        public static TypeReference FromPointer(PointerType type)
        {
            return new TypeReference
            {
                Type = type
            };
        }
        public static TypeReference FromArray(ArrayType type)
        {
            return new TypeReference
            {
                Type = type
            };
        }
    }

    public static class TypeReferenceExtensions
    {
        /// <summary>
        /// 関数ポインタの typedef の名前と型を得る
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static (string, FunctionType) GetFunctionTypeFromTypedef(this BaseType type)
        {
            if (type is TypedefType typedefType)
            {
                if (typedefType.Ref.Type is PointerType pointerType)
                {
                    if (pointerType.Pointee.Type is FunctionType functionType)
                    {
                        return (typedefType.Name, functionType);
                    }
                }
            }
            return default;
        }
    }
}
