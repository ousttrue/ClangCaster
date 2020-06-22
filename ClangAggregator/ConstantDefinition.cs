namespace ClangAggregator
{
    public class ConstantDefinition
    {
        public uint Hash { get; private set; }

        public FileLocation Location { get; set; }

        public string Name { get; private set; }

        public string[] Values { get; private set; }

        public string Value
        {
            get
            {
                if (Values.Length == 1)
                {
                    return Values[0];
                }
                else
                {
                    return string.Join(" ", Values);
                }
            }
        }

        public ConstantDefinition(uint hash, ClangLocation location, string name, string[] values)
        {
            Hash = hash;
            Location = new FileLocation(ClangString.FromFile(location.file).ToString(), location.line, location.column, location.begin, location.end);
            Name = name;
            Values = values;
        }

        public override string ToString()
        {
            var values = string.Join(" ", Values);
            return $"{Name} := {values}";
        }
    }
}
