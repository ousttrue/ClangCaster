using System;
using System.Collections.Generic;
using System.Linq;
using libclang;

namespace ClangCaster.Types
{
    public struct EnumValue
    {
        public readonly string Name;
        public readonly uint Value;

        public EnumValue(string name, uint value)
        {
            Name = name;
            Value = value;
        }
    }

    public class EnumType : UserType
    {
        public List<EnumValue> Values = new List<EnumValue>();

        EnumType((uint, ClangLocation, string) args) : base(args)
        {
        }

        public override string ToString()
        {
            return $"enum {Name} {{{string.Join(", ", Values.Select(x => x.Name))}}}";
        }

        public static EnumType Parse(in CXCursor cursor)
        {
            var type = new EnumType(cursor.CursorHashLocationSpelling());

            ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
            {
                switch (child.kind)
                {
                    case CXCursorKind._EnumConstantDecl:
                        using (var childName = ClangString.FromCursor(child))
                        {
                            var childValue = index.clang_getEnumConstantDeclUnsignedValue(child);
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
    }
}
