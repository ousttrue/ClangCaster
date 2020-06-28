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

        public string[] DefaultParamTokens;

        public FunctionParam(int index, string name, TypeReference typeRef, string[] defaultParamTokens)
        {
            Index = index;
            Name = name;
            Ref = typeRef;
            IsLast = false;
            DefaultParamTokens = defaultParamTokens;
        }

        public FunctionParam MakeLast()
        {
            return new FunctionParam(Index, Name, Ref, DefaultParamTokens)
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

        public int VTableIndex = -1;

        public bool IsDelegate;

        FunctionType(string name) : base(name)
        {
        }

        public override string ToString()
        {
            var args = string.Join(", ", Params.Select(x => x.Ref.Type));
            return $"{Result.Type} {Name}({args});";
        }

        public static FunctionType Parse(in CXCursor cursor, TypeMap typeMap, in CXType resultType)
        {
            var type = new FunctionType(cursor.Spelling());

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
                            var values = getDefaultValue(child);
                            type.Params.Add(new FunctionParam(type.Params.Count, childName, typeRef, values));
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

        static string[] getDefaultValue(in CXCursor cursor)
        {
            var tu = libclang.clang_Cursor_getTranslationUnit(cursor);
            using (var token = new ClangToken(cursor))
            {
                var tokenSpellings = token.Select(x => x.Spelling(token.TU)).ToArray();
                for (int i = 0; i < tokenSpellings.Length; ++i)
                {
                    if (tokenSpellings[i] == "=")
                    {
                        return tokenSpellings.Skip(i + 1).ToArray();
                    }
                }
            }

            // ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
            // {
            //     switch (child.kind)
            //     {
            //         case CXCursorKind._TypeRef:
            //             {
            //                 // null ?
            //                 // auto referenced = clang_getCursorReferenced(child);
            //                 // debug auto a = 0;
            //                 break;
            //             }

            //         case CXCursorKind._FirstExpr:
            //             {
            //                 // default value. ex: void func(int a = 0);
            //                 // return FirstExpr(child);
            //                 break;
            //             }

            //         case CXCursorKind._IntegerLiteral:
            //             {
            //                 // array length. ex: void func(int a[4]);
            //                 // debug auto a = 0;
            //                 break;
            //             }

            //         default:
            //             throw new NotImplementedException();
            //     }

            //     return CXChildVisitResult._Continue;
            // });

            return default;
        }

        public static FunctionType Parse(in CXCursor cursor, TypeMap typeMap)
        {
            var result = libclang.clang_getCursorResultType(cursor);
            return Parse(cursor, typeMap, result);
        }
    }
}
