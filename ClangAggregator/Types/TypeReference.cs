namespace ClangAggregator.Types
{
    public class TypeReference
    {
        public BaseType Type { get; set; }

        public uint Hash { get; private set; }

        public FileLocation Location { get; set; }

        public uint Count { get; set; }

        public TypeReference(uint hash, ClangLocation location, BaseType type)
        {
            Hash = hash;
            Location = new FileLocation(ClangString.FromFile(location.file).ToString(), location.line, location.column, location.begin, location.end);
            Type = type;
        }

        public TypeReference((uint, ClangLocation) args, BaseType type) : this(args.Item1, args.Item2, type)
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
}
