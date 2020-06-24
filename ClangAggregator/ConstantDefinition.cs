using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClangAggregator
{
    public class ConstantDefinition
    {
        public uint Hash { get; private set; }

        public FileLocation Location { get; set; }

        public string Name;

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

        public ConstantDefinition(uint hash, FileLocation location, string name, IEnumerable<string> values)
        {
            Hash = hash;
            Location = location;
            Name = name;
            Values = values.ToList();
        }

        public override string ToString()
        {
            var values = string.Join(" ", Values);
            return $"{Name} := {values}";
        }

        static string SkipPrefix(string src, string prefix, bool exception)
        {
            if (src.StartsWith(prefix))
            {
                // skip prefix. keep underscore for digits starts
                return src.Substring(prefix.Length - 1);
            }
            else
            {
                if (exception)
                {
                    throw new Exception();
                }
                return src;
            }
        }

        /// <summary>
        /// 前処理
        /// </summary>
        public void Prepare(string prefix)
        {
            Name = SkipPrefix(Name, prefix, true);

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

            // skip prefix
            for (int i = 0; i < Values.Count; ++i)
            {
                Values[i] = SkipPrefix(Values[i], prefix, false);

                if (Values[i] == "SHORT")
                {
                    Values[i] = "short";
                }
            }
        }
    }
}
