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
        protected UserType(uint hash, string name) : base(name)
        {
            Hash = hash;
        }
    }
}
