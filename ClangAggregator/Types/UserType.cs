namespace ClangAggregator.Types
{
    public class UserType : BaseType
    {
        public uint Hash;

        public FileLocation Location;

        protected UserType(uint hash, ClangLocation location, string name) : base(name)
        {
            Hash = hash;
            Location = new FileLocation(ClangString.FromFile(location.file).ToString(), location.line, location.column, location.begin, location.end);
        }

        protected UserType((uint, ClangLocation, string) args) : this(args.Item1, args.Item2, args.Item3)
        {
        }
    }
}
