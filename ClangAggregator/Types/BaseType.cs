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

        public bool IsReference;

        public PointerType(TypeReference pointee, bool isReference = false) : base("Pointer")
        {
            Pointee = pointee;
            IsReference = isReference;
        }
    }

    public class ArrayType : BaseType
    {
        public readonly TypeReference Element;

        public readonly int Size;

        public ArrayType(TypeReference element, int size) : base("Array")
        {
            Element = element;
            Size = size;
        }
    }
}
