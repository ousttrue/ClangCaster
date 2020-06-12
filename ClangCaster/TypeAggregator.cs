using System;
using System.Collections.Generic;
using ClangCaster.Types;
using libclang;

namespace ClangCaster
{
    public class TypeAggregator
    {
        Dictionary<uint, UserType> m_typeMap = new Dictionary<uint, UserType>();

        public void Process(in CXCursor cursor)
        {
            TraverseChildren(cursor, default);
        }

        struct Context
        {
            public readonly int Level;

            public Context(int level)
            {
                Level = level;
            }

            public Context Child()
            {
                return new Context(Level + 1);
            }

            public Context Enter(StructType type)
            {
                return new Context(Level + 1);
            }
        }

        void TraverseChildren(in CXCursor cursor, in Context _context)
        {
            var context = _context;
            ClangVisitor.ProcessChildren(cursor, (in CXCursor c) => Traverse(c, context));
        }

        CXChildVisitResult Traverse(in CXCursor cursor, in Context context)
        {
            // using (var spelling = ClangString.FromCursor(cursor))
            // {
            //     Console.WriteLine($"{cursor.kind}: {spelling}");
            // }

            switch (cursor.kind)
            {
                case CXCursorKind._InclusionDirective:
                case CXCursorKind._ClassTemplate:
                case CXCursorKind._ClassTemplatePartialSpecialization:
                case CXCursorKind._FunctionTemplate:
                case CXCursorKind._UsingDeclaration:
                case CXCursorKind._StaticAssert:
                    // skip
                    break;

                case CXCursorKind._MacroDefinition:
                    // parseMacroDefinition(cursor);
                    break;

                case CXCursorKind._MacroExpansion:
                    {
                        //     ScopedCXString spelling(clang_getCursorSpelling(cursor));
                        //     if (spelling.str_view() == "DEFINE_GUID")
                        //     {
                        //         //   auto tokens = getTokens(cursor);
                        //         //   scope(exit)
                        //         //       clang_disposeTokens(tu, tokens.ptr, cast(uint)
                        //         //       tokens.length);
                        //         //   string[] tokenSpellings =
                        //         //       tokens.map !(t = > tokenToString(cursor,
                        //         //       t)).array();
                        //         //   if (tokens.length == 26) {
                        //         //     auto name = tokenSpellings[2];
                        //         //     if (name.startsWith("IID_")) {
                        //         //       name = name[4.. $];
                        //         //     }
                        //         //     m_uuidMap[name] = tokensToUUID(tokenSpellings[4..
                        //         //     $]);
                        //         //   } else {
                        //         //     debug auto a = 0;
                        //         //   }
                        //     }
                    }
                    break;

                case CXCursorKind._Namespace:
                    {
                        // auto decl = getDecl<Namespace>(cursor);
                        // if (!decl)
                        // {
                        //     auto hash = clang_hashCursor(cursor);
                        //     auto location = Location::get(cursor);
                        //     ScopedCXString spelling(clang_getCursorSpelling(cursor));
                        //     decl = Namespace::create(hash, location.path(), location.line, spelling.str_view());
                        //     pushDecl(cursor, decl);
                        // }
                        // var child = context.EnterNamespace(decl);
                        // TraverseChildren(cursor, child);
                    }
                    break;

                case CXCursorKind._UnexposedDecl:
                    {
                        // ScopedCXTokens tokens(cursor);
                        var child = context.Child();
                        // if (tokens.size() >= 2)
                        // {
                        //     // extern C
                        //     if (tokens.spelling(0).str_view() == "extern" && tokens.spelling(1).str_view() == "\"C\"")
                        //     {
                        //         child.isExternC = true;
                        //     }
                        // }
                        TraverseChildren(cursor, child);
                    }
                    break;

                case CXCursorKind._TypedefDecl:
                    {
                        var type = TypedefType.Parse(cursor);
                        var underlying = index.clang_getTypedefDeclUnderlyingType(cursor);
                        var isConst = index.clang_isConstQualifiedType(underlying);
                        if (Types.PrimitiveType.TryGetPrimitiveType(underlying, out Types.PrimitiveType primitive))
                        {
                            type.Ref = new TypeReference
                            {
                                IsConst = isConst,
                                Type = primitive,
                            };
                        }
                        Console.WriteLine(type);
                        AddType(type);
                    }
                    break;

                case CXCursorKind._FunctionDecl:
                    {
                        // auto decl = parseFunction(cursor, clang_getCursorResultType(cursor));
                        // if (decl)
                        {
                            // auto header = getOrCreateHeader(cursor);
                            // header.types ~ = decl;
                        }
                    }
                    break;

                case CXCursorKind._StructDecl:
                case CXCursorKind._ClassDecl:
                case CXCursorKind._UnionDecl:
                    {
                        var type = GetType(cursor) as StructType;
                        if (type is null)
                        {
                            type = StructType.Parse(cursor);
                            // decl.namespace = context.namespace;
                            type.IsUnion = cursor.kind == CXCursorKind._UnionDecl;
                            type.IsForwardDecl = StructType.IsForwardDeclaration(cursor);
                            AddType(type);
                            // Console.WriteLine(type);

                            if (type.IsForwardDecl)
                            {
                                var defCursor = index.clang_getCursorDefinition(cursor);
                                if (index.clang_equalCursors(defCursor, index.clang_getNullCursor()))
                                {
                                    // not exists
                                }
                                else
                                {
                                    var defDecl = GetType(defCursor) as StructType;
                                    if (defDecl is null)
                                    {
                                        // create
                                        defDecl = StructType.Parse(defCursor);
                                        AddType(defDecl);
                                    }
                                    type.Definition = defDecl;
                                }
                            }
                            else
                            {
                                // push before fields
                                // auto header = getOrCreateHeader(cursor);
                                // header.types ~ = decl;

                                // fields
                                var child = context.Enter(type);
                                // ProcessChildren(cursor, 
                                //                 std::bind(&TraverserImpl::parseStructField, this, decl, std::placeholders::_1, childContext));
                                TraverseChildren(cursor, child);
                            }
                        }
                    }
                    break;

                case CXCursorKind._EnumDecl:
                    {
                        var type = EnumType.Parse(cursor);
                        AddType(type);
                    }
                    break;

                case CXCursorKind._VarDecl:
                    break;

                default:
                    throw new NotImplementedException("unknown CXCursorKind");
            }

            return CXChildVisitResult._Continue;
        }

        UserType GetType(CXCursor cursor)
        {
            var hash = index.clang_hashCursor(cursor);
            if (!m_typeMap.TryGetValue(hash, out UserType type))
            {
                return null;
            }
            return type;
        }

        void AddType(UserType type)
        {
            m_typeMap.Add(type.Hash, type);
        }
    }
}
