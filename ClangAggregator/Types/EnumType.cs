using System;
using System.Collections.Generic;
using System.Linq;
using CIndex;

namespace ClangAggregator.Types
{
    public struct EnumValue
    {
        public string Name { get; private set; }
        public uint Value { get; private set; }

        public string Hex
        {
            get
            {
                if (Value < int.MaxValue)
                {
                    return $"0x{Value.ToString("x")}";
                }
                else
                {
                    return $"unchecked((int)0x{Value.ToString("x")})";
                }
            }
        }

        public EnumValue(string name, uint value)
        {
            Name = name;
            Value = value;
        }
    }

    public class EnumType : UserType
    {
        public List<EnumValue> Values { get; private set; }

        EnumType(string name) : base(name)
        {
            Values = new List<EnumValue>();
        }

        public override string ToString()
        {
            return $"enum {Name} {{{string.Join(", ", Values.Select(x => x.Name))}}}";
        }

        public static EnumType Parse(in CXCursor cursor)
        {
            var type = new EnumType(cursor.Spelling());

            ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
            {
                switch (child.kind)
                {
                    case CXCursorKind._EnumConstantDecl:
                        using (var childName = ClangString.FromCursor(child))
                        {
                            var childValue = libclang.clang_getEnumConstantDeclUnsignedValue(child);
                            if (childValue > uint.MaxValue)
                            {
                                throw new NotImplementedException();
                            }
                            type.Values.Add(new EnumValue(childName.ToString(), (uint)childValue));
                        }
                        break;

                    default:
                        throw new NotImplementedException("parse enum unknown");
                }

                return CXChildVisitResult._Continue;
            });

            return type;
        }

        static string GetShared(string l, string r)
        {
            var lLen = l.Length;
            var rLen = r.Length;
            var len = Math.Min(lLen, rLen);
            int i = 0;
            for (; i < len; ++i)
            {
                if (l[i] != r[i])
                {
                    break;
                }
            }

            return l.Substring(0, i);
        }

        /// <summary>
        /// 値の共通部分を除去する
        /// </summary>
        public void PreparePrefix()
        {
            if (Values.Count == 0)
            {
                return;
            }

            string shared = Values[0].Name;
            for (int i = 0; i < Values.Count; ++i)
            {
                shared = GetShared(Values[i].Name, shared);
            }

            if (shared.Length == 0)
            {
                return;
            }

            var len = shared.LastIndexOf('_');
            if (len == -1)
            {
                return;
            }

            // replace values
            Values = Values.Select(x => new EnumValue(x.Name.Substring(len), x.Value)).ToList();
        }
    }
}
