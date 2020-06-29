using System;
using System.Collections.Generic;
using System.Linq;

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

        public static ConstantDefinition Create(uint hash, FileLocation location, IEnumerable<string> tokens)
        {
            var name = tokens.First();
            if (name == "CINDEX_VERSION")
            {
                var values = tokens.Skip(1).ToArray();
                // CINDEX_VERSION_ENCODE(major, minor)
                if (values.Length != 6)
                {
                    throw new Exception();
                }
                return new ConstantDefinition(hash, location, name, new string[]{
                    "CINDEX_VERSION_MAJOR", "*", "10000", "+", "CINDEX_VERSION_MINOR"
                });
            }
            if (name == "CINDEX_VERSION_STRING")
            {

            }
            return new ConstantDefinition(hash, location, name, tokens.Skip(1).ToArray());
        }

        public override string ToString()
        {
            var values = string.Join(" ", Values);
            return $"{Name} := {values}";
        }

        public static bool IsAlphabet(char c)
        {
            return (c >= 'A' && c <= 'z');
        }

        public bool IsRename => Values.Count == 1 && IsAlphabet(Values[0][0]);

        static string[] CastTypes = new string[]
        {
            "LPWSTR",
            "ULONG_PTR",
            "UINT_PTR",
            "HWND",
            "HBITMAP",
            "HANDLE",
            "DWORD",
            "LONG",
            "ID3DInclude",
            "ImDrawCallback",
        };

        IEnumerable<(int, int)> GetParenthesis()
        {
            var stack = new Stack<int>();
            for (int i = 0; i < Values.Count; ++i)
            {
                switch (Values[i])
                {
                    case "(":
                        stack.Push(i);
                        break;

                    case ")":
                        yield return (stack.Pop(), i);
                        break;
                }
            }
        }

        bool IsMacrofunctionCall(int open, int close)
        {
            if (open == 0)
            {
                return false;
            }
            var name = Values[open - 1];
            if (name == "sizeof")
            {
                return false;
            }

            var result = name.All(IsAlphabet);
            return result;
        }

        bool IsCast(int open, int close)
        {
            // ( Axxx )
            if (open + 2 == close)
            {
                return CastTypes.Contains(Values[open + 1]);
            }
            else if (open + 3 == close)
            {
                // ID3DInclude *
                if (Values[open + 1] == "-")
                {
                    return false;
                }
                return CastTypes.Contains(Values[open + 1]) && Values[open + 2] == "*";
            }
            else
            {
                return false;
            }
        }

        void RemoveCastOrMacroFunction()
        {
            while (true)
            {
                var found = false;
                foreach (var (open, close) in GetParenthesis())
                {
                    if (IsMacrofunctionCall(open, close))
                    {
                        Values.RemoveAt(close);
                        Values.RemoveRange(open - 1, 2);
                        found = true;
                        break;
                    }
                    if (IsCast(open, close))
                    {
                        Values.RemoveRange(open, 1 + close - open);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    break;
                }
            }
        }

        static Dictionary<string, string> s_ReplaceMap = new Dictionary<string, string>{
            {"SHORT", "short"},
            {"UINT_MAX", "uint.MaxValue"},
        };

        /// <summary>
        /// 0x1234L => 0x1234
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        static bool TryDropL(string src, out string dst)
        {
            if (src.Last() == 'l' || src.Last() == 'L')
            {
                if (src.StartsWith("0x") || src.StartsWith("0X"))
                {
                    dst = src.Substring(0, src.Length - 1);
                    return true;
                }

                if (src.Take(src.Length - 1).All(x => Char.IsDigit(x)))
                {
                    dst = src.Substring(0, src.Length - 1);
                    return true;
                }
            }

            dst = default;
            return false;
        }

        /// <summary>
        /// 前処理
        /// </summary>
        public bool Prepare()
        {
            // black list
            if (Name == "CINDEX_VERSION_STRING")
            {
                return false;
            }
            if (Values.Contains("IM_COL32"))
            {
                return false;
            }

            // compile time
            if (Values.Contains("sizeof"))
            {
                return false;
            }

            // string
            if (Values.Any(x => x.Contains('"')))
            {
                // non int
                return false;
            }

            RemoveCastOrMacroFunction();

            for (int i = 0; i < Values.Count; ++i)
            {
                if (s_ReplaceMap.TryGetValue(Values[i], out string replace))
                {
                    Values[i] = replace;
                }

                if (TryDropL(Values[i], out string dropped))
                {
                    Values[i] = dropped;
                }
            }

            return true;
        }
    }
}
