namespace ClangCaster.Types
{
    public class TypedefType : UserType
    {
        public TypeReference Ref;

        public TypedefType((uint, ClangLocation, string) args) : base(args)
        { }

        public override string ToString()
        {
            return $"typedef {Name} = {Ref.Type}";
        }
    }
}
