namespace ClangCaster.Types
{
    public class BaseType
    {
        public string Name;
        protected BaseType(string name)
        {
            Name = name;
        }
    }

    public class UserType : BaseType
    {
        public uint Hash;

        public ClangLocation Location;

        protected UserType(uint hash, ClangLocation location, string name) : base(name)
        {
            Hash = hash;
            Location = location;
        }

        protected UserType((uint, ClangLocation, string) args) : this(args.Item1, args.Item2, args.Item3)
        {
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
}
