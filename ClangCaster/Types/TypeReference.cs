namespace ClangCaster.Types
{
    public struct TypeReference
    {
        public readonly bool IsConst;
        public readonly BaseType Type;

        public TypeReference(BaseType type, bool isConst = false)
        {
            Type = type;
            IsConst = isConst;
        }
    }
}
