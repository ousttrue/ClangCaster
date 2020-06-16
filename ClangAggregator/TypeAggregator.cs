using System;
using ClangAggregator.Types;
using libclang;

namespace ClangAggregator
{
    /// <summary>
    /// CXCursorを辿って型を集める
    /// </summary>
    public class TypeAggregator
    {
        TypeMap m_typeMap = new TypeMap();

        public TypeMap Process(in CXCursor cursor)
        {
            TraverseChildren(cursor, default);
            return m_typeMap;
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
            switch (cursor.kind)
            {
                case CXCursorKind._InclusionDirective:
                case CXCursorKind._ClassTemplate:
                case CXCursorKind._ClassTemplatePartialSpecialization:
                case CXCursorKind._FunctionTemplate:
                case CXCursorKind._UsingDeclaration:
                case CXCursorKind._StaticAssert:
                case CXCursorKind._FieldDecl:
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
                        var nested = context.Child();
                        // if (tokens.size() >= 2)
                        // {
                        //     // extern C
                        //     if (tokens.spelling(0).str_view() == "extern" && tokens.spelling(1).str_view() == "\"C\"")
                        //     {
                        //         child.isExternC = true;
                        //     }
                        // }
                        TraverseChildren(cursor, nested);
                    }
                    break;

                case CXCursorKind._TypedefDecl:
                    {
                        // first
                        // TraverseChildren(cursor, context);

                        var type = TypedefType.Parse(cursor, m_typeMap);
                        m_typeMap.Add(type);
                    }
                    break;

                case CXCursorKind._FunctionDecl:
                    {
                        var type = FunctionType.Parse(cursor, m_typeMap);
                        m_typeMap.Add(type);
                    }
                    break;

                case CXCursorKind._StructDecl:
                case CXCursorKind._ClassDecl:
                case CXCursorKind._UnionDecl:
                    {
                        var type = m_typeMap.Get(cursor) as StructType;
                        if (type is null)
                        {
                            type = StructType.Parse(cursor, m_typeMap);
                            // decl.namespace = context.namespace;
                            m_typeMap.Add(type);

                            // if (!StructType.IsForwardDeclaration(cursor))
                            {
                                var nested = context.Child();
                                TraverseChildren(cursor, nested);

                                type.ParseFields(cursor, m_typeMap);
                            }
                        }
                        else
                        {
                            // throw new NotImplementedException();
                        }
                    }
                    break;

                case CXCursorKind._EnumDecl:
                    {
                        var type = EnumType.Parse(cursor);
                        m_typeMap.Add(type);
                    }
                    break;

                case CXCursorKind._VarDecl:
                    break;

                default:
                    throw new NotImplementedException("unknown CXCursorKind");
            }

            return CXChildVisitResult._Continue;
        }
    }
}
