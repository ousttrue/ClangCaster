namespace ClangAggregator.Types
{
    public struct TypeReference
    {
        public readonly bool IsConst;
        public BaseType Type { get; private set; }

        public TypeReference(BaseType type, bool isConst = false)
        {
            Type = type;
            IsConst = isConst;
        }
    }
}
