namespace ClangAggregator.Types
{
    public class BaseType
    {
        public string Name { get; set; }

        protected BaseType(string name)
        {
            Name = name;
        }
    }

    public class PointerType : BaseType
    {
        public TypeReference Pointee;

        public PointerType(in TypeReference pointee) : base("Pointer")
        {
            Pointee = pointee;
        }

        public PointerType(BaseType type, bool isConst) : this(new TypeReference(type, isConst))
        {
        }
    }

    public class ArrayType : BaseType
    {
        public TypeReference Element;

        public ArrayType(in TypeReference element) : base("Array")
        {
            Element = element;
        }

        public ArrayType(BaseType type, bool isConst) : this(new TypeReference(type, isConst))
        {
        }
    }
}
