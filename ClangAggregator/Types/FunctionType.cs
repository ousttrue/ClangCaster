using System;
using System.Collections.Generic;
using System.Linq;
using CIndex;

namespace ClangAggregator.Types
{
    public struct FunctionParam
    {
        public readonly int Index;
        public readonly string Name;
        public readonly TypeReference Ref;

        public bool IsLast { get; private set; }

        public FunctionParam(int index, string name, TypeReference typeRef)
        {
            Index = index;
            Name = name;
            Ref = typeRef;
            IsLast = false;
        }

        public FunctionParam MakeLast()
        {
            return new FunctionParam(Index, Name, Ref)
            {
                IsLast = true
            };
        }
    }

    public class FunctionType : UserType
    {
        public bool IsVariadic;

        public bool HasBody;

        public bool DllExport;

        public TypeReference Result;

        public List<FunctionParam> Params = new List<FunctionParam>();

        FunctionType((uint, ClangLocation, string) args) : base(args)
        {
        }

        public override string ToString()
        {
            var args = string.Join(", ", Params.Select(x => x.Ref.Type));
            return $"{Result.Type} {Name}({args});";
        }

        public static FunctionType Parse(in CXCursor cursor, TypeMap typeMap, in CXType resultType)
        {
            var type = new FunctionType(cursor.CursorHashLocationSpelling());

            type.Result = typeMap.CxTypeToType(resultType, cursor);

            ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
            {
                var childName = child.Spelling();

                switch (child.kind)
                {
                    case CXCursorKind._CompoundStmt:
                        type.HasBody = true;
                        break;

                    case CXCursorKind._TypeRef:
                        break;

                    case CXCursorKind._WarnUnusedResultAttr:
                        break;

                    case CXCursorKind._ParmDecl:
                        {
                            var childType = libclang.clang_getCursorType(child);
                            var typeRef = typeMap.CxTypeToType(childType, child);
                            type.Params.Add(new FunctionParam(type.Params.Count, childName, typeRef));
                        }
                        break;

                    case CXCursorKind._DLLImport:
                    case CXCursorKind._DLLExport:
                        type.DllExport = true;
                        break;

                    case CXCursorKind._UnexposedAttr:
                        break;

                    default:
                        throw new NotImplementedException("unknown param type");
                }

                return CXChildVisitResult._Continue;
            });

            if (type.Params.Any())
            {
                type.Params[type.Params.Count - 1] = type.Params[type.Params.Count - 1].MakeLast();
            }
            return type;
        }

        public static FunctionType Parse(in CXCursor cursor, TypeMap typeMap)
        {
            var result = libclang.clang_getCursorResultType(cursor);
            return Parse(cursor, typeMap, result);
        }
    }
}
