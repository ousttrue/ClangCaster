using System;
using ClangCaster.Types;
using libclang;

namespace ClangCaster
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
                        var type = new TypedefType(cursor.CursorHashLocationSpelling());
                        var underlying = index.clang_getTypedefDeclUnderlyingType(cursor);
                        type.Ref = m_typeMap.CxTypeToType(underlying, cursor);
                        m_typeMap.Add(type);
                    }
                    break;

                case CXCursorKind._FunctionDecl:
                    {
                        var type = new FunctionType(cursor.CursorHashLocationSpelling());
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
                            type = new StructType(cursor.CursorHashLocationSpelling());
                            // decl.namespace = context.namespace;
                            type.IsUnion = cursor.kind == CXCursorKind._UnionDecl;
                            type.IsForwardDecl = StructType.IsForwardDeclaration(cursor);
                            m_typeMap.Add(type);

                            if (type.IsForwardDecl)
                            {
                                var defCursor = index.clang_getCursorDefinition(cursor);
                                if (index.clang_equalCursors(defCursor, index.clang_getNullCursor()))
                                {
                                    // not exists
                                }
                                else
                                {
                                    var defDecl = m_typeMap.Get(defCursor) as StructType;
                                    if (defDecl is null)
                                    {
                                        // create
                                        defDecl = new StructType(defCursor.CursorHashLocationSpelling());
                                        m_typeMap.Add(defDecl);
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
