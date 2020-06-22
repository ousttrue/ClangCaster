using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClangAggregator
{
    public class ConstantDefinition
    {
        public uint Hash { get; private set; }

        public FileLocation Location { get; set; }

        public string Name { get; private set; }

        public List<string> Values { get; private set; }

        public string Value
        {
            get
            {
                if (Values.Count == 1)
                {
                    return Values[0];
                }
                else
                {
                    return string.Join(" ", Values);
                }
            }
        }

        public ConstantDefinition(uint hash, ClangLocation location, string name, IEnumerable<string> values)
        {
            Hash = hash;
            Location = new FileLocation(ClangString.FromFile(location.file).ToString(), location.line, location.column, location.begin, location.end);
            Name = name;
            Values = values.ToList();
        }

        public override string ToString()
        {
            var values = string.Join(" ", Values);
            return $"{Name} := {values}";
        }

        /// <summary>
        /// 前処理
        /// </summary>
        public void Prepare()
        {
            if (Values.Count >= 4)
            {
                if (Values[0] == "_HRESULT_TYPEDEF_" && Values[1] == "(" && Values.Last() == ")")
                {
                    Values.RemoveRange(0, 2);
                    Values.RemoveAt(Values.Count - 1);
                }
            }

            if (Values.Count == 1)
            {
                if (Values[0].Last() == 'L')
                {
                    // drop L
                    Values[0] = Values[0].Substring(0, Values[0].Length - 1);
                }
            }
        }

        public string Render()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"       // {Location.Path.Path}:{Location.Line}");
            sb.AppendLine($"       public const int {Name} = {Value};");

            return sb.ToString();
        }
    }
}
