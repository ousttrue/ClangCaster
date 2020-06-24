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
        public uint SizeOf { get; private set; }
        public bool IsUnion { get; private set; }
        public List<StructField> Fields { get; private set; }

        public TypeReference BaseClass;
        public Guid IID;

        StructType(string name) : base(name)
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
            var type = new StructType(cursor.Spelling());
            type.IsUnion = cursor.kind == CXCursorKind._UnionDecl;
            type.SizeOf = (uint)libclang.clang_Type_getSizeOf(libclang.clang_getCursorType(cursor));
            return type;
        }

        const string D3D11_KEY = "MIDL_INTERFACE(\"";
        const string D2D1_KEY = "DX_DECLARE_INTERFACE(\"";
        const string DWRITE_KEY = "DWRITE_DECLARE_INTERFACE(\"";

        static string ExtractIID(string src, string key)
        {
            return src.Substring(key.Length, src.Length - key.Length - 2);
        }

        static bool TryGetIID(string src, out Guid iid)
        {
            if (src.StartsWith(D3D11_KEY))
            {
                iid = Guid.Parse(ExtractIID(src, D3D11_KEY));
                return true;
            }
            else if (src.StartsWith(D2D1_KEY))
            {
                iid = Guid.Parse(ExtractIID(src, D2D1_KEY));
                return true;
            }
            else if (src.StartsWith(DWRITE_KEY))
            {
                iid = Guid.Parse(ExtractIID(src, DWRITE_KEY));
                return true;
            }

            iid = default;
            return false;
        }

        /// <summary>
        /// third
        /// </summary>
        /// <param name="cursor"></param>
        /// <param name="typeMap"></param>
        public void ParseFields(in CXCursor cursor, TypeMap typeMap)
        {
            if (Fields.Any())
            {
                // not reach here
                throw new Exception();
            }

            ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
            {

                switch (child.kind)
                {
                    case CXCursorKind._FieldDecl:
                        {
                            var fieldName = child.Spelling();
                            var fieldOffset = (uint)libclang.clang_Cursor_getOffsetOfField(child);
                            var fieldType = libclang.clang_getCursorType(child);
                            if (!string.IsNullOrEmpty(fieldName) && Fields.Any(x => x.Name == fieldName))
                            {
                                throw new Exception();
                            }
                            Fields.Add(new StructField(Fields.Count, fieldName, typeMap.CxTypeToType(fieldType, child), fieldOffset));
                            break;
                        }

                    case CXCursorKind._UnexposedAttr:
                        {
                            var src = typeMap.GetSource(child);
                            if (TryGetIID(src, out Guid iid))
                            {
                                IID = iid;
                            }
                        }
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
                            var referenced = libclang.clang_getCursorReferenced(child);
                            var baseClass = typeMap.GetOrCreate(referenced);
                            BaseClass = baseClass;
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
            if (libclang.clang_equalCursors(definition, libclang.clang_getNullCursor()) != 0)
            {
                return true;
            }

            // If there is a definition, then the forward declaration and the definition
            // are in the same translation unit. This cursor is the forward declaration if
            // it is _not_ the definition.
            return libclang.clang_equalCursors(cursor, definition) != 0;
        }

        public static StructType CreatePointerStructType(string name)
        {
            var structType = new StructType(name);
            var voidPtr = new PointerType(TypeReference.FromPrimitive(VoidType.Instance));
            structType.Fields.Add(new StructField(0, "ptr", TypeReference.FromPointer(voidPtr), 0));
            return structType;
        }
    }
}
