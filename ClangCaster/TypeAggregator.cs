using System;
using System.Runtime.InteropServices;
using libclang;

namespace ClangCaster
{
    public class TypeAggregator
    {
        struct Context
        {
        }

        void TraverseChildren(in CXCursor cursor, in Context _context)
        {
            var context = _context;
            ClangVisitor.ProcessChildren(cursor, (in CXCursor c) => Traverse(c, context));
        }

        public void Process(in CXCursor cursor)
        {
            TraverseChildren(cursor, default);
        }

        CXChildVisitResult Traverse(in CXCursor cursor, in Context context)
        {
            using (var spelling = new ClangString(cursor))
            {
                Console.WriteLine($"{cursor.kind}: {spelling}");
            }

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

                        // auto child = context.createChild();
                        // if (tokens.size() >= 2)
                        // {
                        //     // extern C
                        //     if (tokens.spelling(0).str_view() == "extern" && tokens.spelling(1).str_view() == "\"C\"")
                        //     {
                        //         child.isExternC = true;
                        //     }
                        // }

                        // TraverseChildren(cursor, child);
                    }
                    break;

                case CXCursorKind._TypedefDecl:
                    // parseTypedef(cursor);
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
                    // parseStruct(cursor, context, false);
                    break;

                case CXCursorKind._UnionDecl:
                    // parseStruct(cursor, context, true);
                    break;

                case CXCursorKind._EnumDecl:
                    // parseEnum(cursor);
                    break;

                case CXCursorKind._VarDecl:
                    break;

                default:
                    throw new Exception("unknown CXCursorKind");
            }

            return CXChildVisitResult._Continue;
        }
    }
}
