namespace ClangAggregator.Types
{
    public class UserType : BaseType
    {
        public uint Hash { get; private set; }

        public FileLocation Location { get; set; }

        public uint Count { get; set; }

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
