using System;
using ClangAggregator.Types;
using CIndex;
using System.Linq;

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

            public readonly StructType Current;

            public Context(int level, StructType current)
            {
                Level = level;
                Current = current;
            }

            public Context Child()
            {
                return new Context(Level + 1, Current);
            }

            public Context Enter(StructType type)
            {
                return new Context(Level + 1, type);
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
                case CXCursorKind._FirstExpr:
                case CXCursorKind._AlignedAttr:
                case CXCursorKind._CXXAccessSpecifier:
                case CXCursorKind._Constructor:
                case CXCursorKind._Destructor:
                case CXCursorKind._ConversionFunction:
                    // skip
                    break;

                case CXCursorKind._MacroDefinition:
                    m_typeMap.ParseMacroDefinition(cursor);
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
                        var reference = m_typeMap.GetOrCreate(cursor);
                        reference.Type = TypedefType.Parse(cursor, m_typeMap);
                    }
                    break;

                case CXCursorKind._FunctionDecl:
                    {
                        var reference = m_typeMap.GetOrCreate(cursor);
                        reference.Type = FunctionType.Parse(cursor, m_typeMap);
                    }
                    break;

                case CXCursorKind._StructDecl:
                case CXCursorKind._ClassDecl:
                case CXCursorKind._UnionDecl:
                    {
                        var reference = m_typeMap.GetOrCreate(cursor);
                        var structType = StructType.Parse(cursor, m_typeMap);
                        reference.Type = structType;
                        var nested = context.Enter(structType);
                        TraverseChildren(cursor, nested);
                        if (libclang.clang_Cursor_isAnonymousRecordDecl(cursor) != 0)
                        {
                            // anonymous type decl add field to current struct.
                            structType.AnonymousParent = context.Current;
                            var fieldOffset = (uint)libclang.clang_Cursor_getOffsetOfField(cursor);
                            var current = context.Current;
                            // var fieldName = cursor.Spelling();
                            // FIXME: anonymous type field offset ?
                            current.Fields.Add(new StructField(current.Fields.Count, "", reference, 0));
                        }
                    }
                    break;

                case CXCursorKind._FieldDecl:
                    {
                        var fieldName = cursor.Spelling();
                        var fieldOffset = (uint)libclang.clang_Cursor_getOffsetOfField(cursor);
                        var fieldType = libclang.clang_getCursorType(cursor);
                        var current = context.Current;
                        if (!string.IsNullOrEmpty(fieldName) && current.Fields.Any(x => x.Name == fieldName))
                        {
                            throw new Exception();
                        }
                        current.Fields.Add(new StructField(current.Fields.Count, fieldName, m_typeMap.CxTypeToType(fieldType, cursor), fieldOffset));
                        break;
                    }

                case CXCursorKind._CXXBaseSpecifier:
                    {
                        var referenced = libclang.clang_getCursorReferenced(cursor);
                        var baseClass = m_typeMap.GetOrCreate(referenced);
                        context.Current.BaseClass = baseClass;
                    }
                    break;

                case CXCursorKind._UnexposedAttr:
                    {
                        var src = m_typeMap.GetSource(cursor);
                        if (StructType.TryGetIID(src, out Guid iid))
                        {
                            context.Current.IID = iid;
                        }
                    }
                    break;

                case CXCursorKind._CXXMethod:
                    {
                        var method = FunctionType.Parse(cursor, m_typeMap);
                        if (!method.HasBody)
                        {
                            // TODO: override check

                            // IntPtr p = default;
                            // uint n = default;
                            // ulong[] hashes;
                            // libclang.clang_getOverriddenCursors(child, ref p, ref n);
                            // if (n)
                            // {
                            //     scope(exit) clang_disposeOverriddenCursors(p);

                            //     hashes.length = n;
                            //     for (int i = 0; i < n; ++i)
                            //     {
                            //         hashes[i] = clang_hashCursor(p[i]);
                            //     }

                            //     debug
                            //     {
                            //         var childName = getCursorSpelling(child);
                            //         var a = 0;
                            //     }
                            // }

                            // var found = -1;
                            // for (int i = 0; i < VTable.Count; ++i)
                            // {
                            //     var current = decl.vtable[i].hash;
                            //     if (hashes.any!(x = > x == current))
                            //     {
                            //         found = i;
                            //         break;
                            //     }
                            // }
                            // if (found != -1)
                            // {
                            //     debug var a = 0;
                            // }
                            // else
                            // {
                            //     found = cast(int) decl.vtable.length;
                            //     decl.vtable ~ = method;
                            //     debug var a = 0;
                            // }
                            // decl.methodVTableIndices ~ = found;

                            context.Current.Methods.Add(method);
                        }
                    }
                    break;

                case CXCursorKind._EnumDecl:
                    {
                        var reference = m_typeMap.GetOrCreate(cursor);
                        reference.Type = EnumType.Parse(cursor);
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
