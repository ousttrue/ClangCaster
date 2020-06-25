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

        public static bool IsAlphabet(char c)
        {
            return (c >= 'A' && c <= 'z');
        }

        public bool IsRename => Values.Count == 1 && Values[0].All(IsAlphabet);

        // static string SkipPrefix(string src, string prefix, bool exception)
        // {
        //     if (src.StartsWith(prefix))
        //     {
        //         // skip prefix. keep underscore for digits starts
        //         return src.Substring(prefix.Length - 1);
        //     }
        //     else
        //     {
        //         if (exception)
        //         {
        //             throw new Exception();
        //         }
        //         return src;
        //     }
        // }

        // public static string[] RemoveMacroFunctions = new string[]
        // {
        //     "_HRESULT_TYPEDEF_",
        //     "MAKEINTRESOURCE",
        //     "MAKEINTATOM",
        // };

        static string[] CastTypes = new string[]
        {
            "LPWSTR",
            "ULONG_PTR",
            "HWND",
            "HBITMAP",
            "HANDLE",
            "DWORD",
            "LONG",
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
            if(open==0)
            {
                return false;
            }
            var name = Values[open-1];
            if(name=="sizeof")
            {
                return false;
            }

            var result = name.All(IsAlphabet);
            return result;
        }

        bool IsCast(int open, int close)
        {
            // ( Axxx )
            return open + 2 == close && CastTypes.Contains(Values[open + 1]);
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
                        Values.RemoveRange(open, 3);
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
            {"UINT_MAX", "4294967295"},
        };

        /// <summary>
        /// 前処理
        /// </summary>
        public void Prepare()
        {
            // Name = SkipPrefix(Name, prefix, true);

            RemoveCastOrMacroFunction();

            if (Values.Count == 1)
            {
                if (Values[0].Last() == 'L')
                {
                    // drop L
                    Values[0] = Values[0].Substring(0, Values[0].Length - 1);
                }
            }

            for (int i = 0; i < Values.Count; ++i)
            {
                // Values[i] = SkipPrefix(Values[i], prefix, false);

                if (s_ReplaceMap.TryGetValue(Values[i], out string replace))
                {
                    Values[i] = replace;
                }

                // // split
                // foreach (var p in UseConstantPrefixies)
                // {
                //     if (Values[i].StartsWith(p))
                //     {
                //         // split
                //         Values[i] = $"{Values[i][0..p.Length]}.{Values[i][(p.Length - 1)..^0]}";
                //     }
                // }
            }
        }
    }
}
