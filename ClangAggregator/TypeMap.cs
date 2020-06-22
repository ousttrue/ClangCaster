using System;
using System.Collections;
using System.Collections.Generic;
using ClangAggregator.Types;
using CIndex;
using System.Linq;

namespace ClangAggregator
{
    /// <summary>
    /// libclangで解析した型を管理する
    /// </summary>
    public class TypeMap : IEnumerable<KeyValuePair<uint, TypeReference>>
    {
        Dictionary<uint, TypeReference> m_typeMap = new Dictionary<uint, TypeReference>();

        List<ConstantDefinition> m_constants = new List<ConstantDefinition>();
        public IReadOnlyList<ConstantDefinition> Constants => m_constants;

        public TypeReference GetOrCreate(CXCursor cursor)
        {
            var hash = libclang.clang_hashCursor(cursor);
            if (m_typeMap.TryGetValue(hash, out TypeReference type))
            {
                // この型がTypedefなどから参照されている回数
                ++type.Count;
            }
            else
            {
                type = new TypeReference(cursor.CursorHashLocation(), null);
                m_typeMap.Add(hash, type);
            }

            return type;
        }

        public IEnumerator<KeyValuePair<uint, TypeReference>> GetEnumerator()
        {
            return m_typeMap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// cxType から 型を得る。
        /// ポインター、参照等を再帰的に剥がす。
        /// ユーザー定義型の場合は、cursor から参照を辿る。
        /// </summary>
        /// <param name="cxType"></param>
        /// <param name="cursor"></param>
        /// <returns></returns>
        public Types.TypeReference CxTypeToType(in CXType cxType, in CXCursor cursor)
        {
            var isConst = libclang.clang_isConstQualifiedType(cxType);
            if (Types.PrimitiveType.TryGetPrimitiveType(cxType, out Types.PrimitiveType primitive))
            {
                return TypeReference.FromPrimitive(primitive);
            }

            if (cxType.kind == CXTypeKind._Unexposed)
            {
                // nullptr_t
                return TypeReference.FromPointer(new PointerType(TypeReference.FromPrimitive(VoidType.Instance)));
            }

            if (cxType.kind == CXTypeKind._Pointer)
            {
                return TypeReference.FromPointer(new PointerType(CxTypeToType(libclang.clang_getPointeeType(cxType), cursor)));
            }

            if (cxType.kind == CXTypeKind._LValueReference)
            {
                return TypeReference.FromPointer(new PointerType(CxTypeToType(libclang.clang_getPointeeType(cxType), cursor)));
            }

            if (cxType.kind == CXTypeKind._IncompleteArray)
            {
                return TypeReference.FromPointer(new PointerType(CxTypeToType(libclang.clang_getArrayElementType(cxType), cursor)));
            }

            if (cxType.kind == CXTypeKind._ConstantArray)
            {
                var arraySize = (int)libclang.clang_getArraySize(cxType);
                var elementType = CxTypeToType(libclang.clang_getArrayElementType(cxType), cursor);
                return TypeReference.FromArray(new ArrayType(elementType, arraySize));
            }

            if (cxType.kind == CXTypeKind._Typedef || cxType.kind == CXTypeKind._Record)
            {
                // find reference from child cursors
                TypeReference reference = default;
                ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
                {
                    switch (child.kind)
                    {
                        case CXCursorKind._TypeRef:
                            {
                                var referenced = libclang.clang_getCursorReferenced(child);
                                reference = GetOrCreate(referenced);
                                return CXChildVisitResult._Break;
                            }

                        default:
                            {
                                return CXChildVisitResult._Continue;
                            }
                    }
                });
                if (reference is null)
                {
                    var children = cursor.Children();
                    throw new NotImplementedException("Referenced not found");
                }
                else
                {
                    return reference;
                }
            }

            if (cxType.kind == CXTypeKind._Elaborated)
            {
                // typedef struct {} Hoge;
                TypeReference reference = default;
                ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
                {
                    switch (child.kind)
                    {
                        case CXCursorKind._StructDecl:
                        case CXCursorKind._UnionDecl:
                            {
                                reference = GetOrCreate(child);
                                var structType = reference.Type as StructType;
                                if (structType is null)
                                {
                                    throw new NotImplementedException();
                                }

                                if (!StructType.IsForwardDeclaration(child))
                                {
                                    structType.ParseFields(child, this);
                                }

                                return CXChildVisitResult._Break;
                            }

                        case CXCursorKind._EnumDecl:
                            {
                                reference = GetOrCreate(child);
                                if (reference.Type is null)
                                {
                                    throw new NotImplementedException();
                                }
                                return CXChildVisitResult._Break;
                            }

                        case CXCursorKind._TypeRef:
                            {
                                var referenced = libclang.clang_getCursorReferenced(child);
                                reference = GetOrCreate(referenced);
                                return CXChildVisitResult._Break;
                            }

                        default:
                            return CXChildVisitResult._Continue;
                    }
                });
                if (reference is null)
                {
                    var children = cursor.Children();
                    throw new NotImplementedException("Elaborated not found");
                }
                return reference;
            }

            if (cxType.kind == CXTypeKind._FunctionProto)
            {
                var resultType = libclang.clang_getResultType(cxType);
                var functionType = FunctionType.Parse(cursor, this, resultType);
                return new TypeReference(cursor.CursorHashLocation(), functionType);
            }

            throw new NotImplementedException("type not found");
        }

        public void ParseMacroDefinition(in CXCursor cursor)
        {
            var isFunctionLike = libclang.clang_Cursor_isMacroFunctionLike(cursor) != 0;
            if (isFunctionLike)
            {
                return;
            }

            using (var token = new ClangToken(cursor))
            {
                if (token.Length == 1)
                {
                    return;
                }
                var tokens = token.Select(x => x.Spelling(token.TU)).ToArray();

                if (token.Length > 1)
                {
                    var (hash, location) = cursor.CursorHashLocation();
                    if (location.file != IntPtr.Zero)
                    {
                        m_constants.Add(new ConstantDefinition(hash, location, tokens[0], tokens.Skip(1).ToArray()));
                    }
                    else
                    {
                        // Console.WriteLine(string.Join(", ", tokens));
                    }
                }
                else
                {
                    Console.WriteLine(string.Join(", ", tokens));
                }
            }
        }
    }
}
