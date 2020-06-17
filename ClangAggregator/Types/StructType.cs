using System;
using System.Collections.Generic;
using System.Linq;
using CIndex;

namespace ClangAggregator.Types
{
    public struct StructField
    {
        public readonly int Index;
        public readonly string Name;
        public readonly TypeReference Ref;
        public readonly uint Offset;

        public StructField(int index, string name, TypeReference typeRef, uint offset)
        {
            Index = index;
            Name = name;
            Ref = typeRef;
            Offset = offset;
        }
    }

    public class StructType : UserType
    {
        public bool IsUnion;
        // public bool IsForwardDecl;
        // public StructType Definition;

        public List<StructField> Fields { get; private set; }

        StructType((uint, ClangLocation, string) args) : base(args)
        {
            Fields = new List<StructField>();
        }

        public override string ToString()
        {
            // if (IsForwardDecl)
            // {
            //     return $"struct {Name};";
            // }
            // else
            {
                var fields = string.Join(", ", Fields.Select(x => x.Ref.Type));
                return $"struct {Name} {{}}";
            }
        }

        /// <summary>
        /// first, then traverse children for struct local types
        /// </summary>
        /// <param name="cursor"></param>
        /// <param name="typeMap"></param>
        /// <returns></returns>
        public static StructType Parse(in CXCursor cursor, TypeMap typeMap)
        {
            var type = new StructType(cursor.CursorHashLocationSpelling());
            type.IsUnion = cursor.kind == CXCursorKind._UnionDecl;
            // type.IsForwardDecl = IsForwardDeclaration(cursor);

            // if (type.IsForwardDecl)
            // {
            //     var defCursor = libclang.clang_getCursorDefinition(cursor);
            //     if (libclang.clang_equalCursors(defCursor, libclang.clang_getNullCursor()))
            //     {
            //         // not exists
            //     }
            //     else
            //     {
            //         // var defDecl = typeMap.Get(defCursor) as StructType;
            //         // if (defDecl is null)
            //         // {
            //         //     // create
            //         //     defDecl = StructType.Parse(defCursor, typeMap);
            //         //     typeMap.Add(defDecl);
            //         // }
            //         // type.Definition = defDecl;
            //     }
            // }

            return type;
        }

        /// <summary>
        /// third
        /// </summary>
        /// <param name="cursor"></param>
        /// <param name="typeMap"></param>
        public void ParseFields(in CXCursor cursor, TypeMap typeMap)
        {
            ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
            {

                switch (child.kind)
                {
                    case CXCursorKind._FieldDecl:
                        {
                            var fieldName = child.Spelling();
                            var fieldOffset = (uint)libclang.clang_Cursor_getOffsetOfField(child);
                            var fieldType = libclang.clang_getCursorType(child);
                            if (Fields.Any(x => x.Name == fieldName))
                            {
                                throw new Exception();
                            }
                            Fields.Add(new StructField(Fields.Count, fieldName, typeMap.CxTypeToType(fieldType, child), fieldOffset));
                            break;
                        }

                    case CXCursorKind._UnexposedAttr:
                        // {
                        //     var src = getSource(child);
                        //     var uuid = getUUID(src);
                        //     if (!uuid.empty())
                        //     {
                        //         decl.iid = uuid;
                        //     }
                        // }
                        break;

                    case CXCursorKind._CXXMethod:
                        {
                            var method = FunctionType.Parse(child, typeMap);
                            if (!method.HasBody)
                            {
                                // CXCursor *p;
                                // uint32_t n;
                                // ulong[] hashes;
                                // clang_getOverriddenCursors(child, &p, &n);
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
                                // for (int i = 0; i < decl.vtable.length; ++i)
                                // {
                                //     var current = decl.vtable[i].hash;
                                //     if (hashes.any !(x = > x == current))
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
                                // decl.methods ~ = method;
                            }
                        }
                        break;

                    case CXCursorKind._Constructor:
                    case CXCursorKind._Destructor:
                    case CXCursorKind._ConversionFunction:
                    case CXCursorKind._FunctionTemplate:
                    case CXCursorKind._VarDecl:
                    case CXCursorKind._ObjCClassMethodDecl:
                    case CXCursorKind._UnexposedExpr:
                    case CXCursorKind._AlignedAttr:
                    case CXCursorKind._CXXAccessSpecifier:
                        break;

                    case CXCursorKind._CXXBaseSpecifier:
                        {
                            // Decl referenced = getReferenceType(child);
                            // while (true)
                            // {
                            //     var typeDef = cast(TypeDef) referenced;
                            //     if (!typeDef)
                            //         break;
                            //     referenced = typeDef.typeref.type;
                            // }
                            // var base = cast(Struct) referenced;
                            // if (base.definition)
                            // {
                            //     base = base.definition;
                            // }
                            // decl.base = base;
                            // decl.vtable = base.vtable;
                        }
                        break;

                    case CXCursorKind._TypeRef:
                        // template param ?
                        // debug var a = 0;
                        break;

                    // case CXCursorKind._StructDecl:
                    // case CXCursorKind._ClassDecl:
                    // case CXCursorKind._UnionDecl:
                    // case CXCursorKind._TypedefDecl:
                    // case CXCursorKind._EnumDecl: {
                    //     // nested type
                    //     traverse(child, context);
                    //     // var nestName = getCursorSpelling(child);
                    //     // if (nestName == "")
                    //     // {
                    //     //     // anonymous
                    //     //     var fieldOffset = clang_Cursor_getOffsetOfField(child);
                    //     //     var fieldDecl = getDeclFromCursor(child);
                    //     //     var fieldConst = clang_isConstQualifiedType(fieldType);
                    //     //     decl.fields ~ = Field(fieldOffset, nestName, TypeRef(fieldDecl, fieldConst != 0));
                    //     // }
                    // }
                    // break;

                    default:
                        // traverse(child, context);
                        break;
                }

                return CXChildVisitResult._Continue;
            });
        }

        // https://joshpeterson.github.io/identifying-a-forward-declaration-with-libclang
        public static bool IsForwardDeclaration(in CXCursor cursor)
        {
            var definition = libclang.clang_getCursorDefinition(cursor);

            // If the definition is null, then there is no definition in this translation
            // unit, so this cursor must be a forward declaration.
            if (libclang.clang_equalCursors(definition, libclang.clang_getNullCursor())!=0)
            {
                return true;
            }

            // If there is a definition, then the forward declaration and the definition
            // are in the same translation unit. This cursor is the forward declaration if
            // it is _not_ the definition.
            return libclang.clang_equalCursors(cursor, definition)!=0;
        }
    }
}
