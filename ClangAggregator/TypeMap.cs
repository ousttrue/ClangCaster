using System;
using System.Collections;
using System.Collections.Generic;
using ClangAggregator.Types;
using libclang;

namespace ClangAggregator
{
    /// <summary>
    /// libclangで解析した型を管理する
    /// </summary>
    public class TypeMap : IEnumerable<KeyValuePair<uint, UserType>>
    {
        Dictionary<uint, UserType> m_typeMap = new Dictionary<uint, UserType>();

        public UserType Get(CXCursor cursor)
        {
            var hash = index.clang_hashCursor(cursor);
            if (!m_typeMap.TryGetValue(hash, out UserType type))
            {
                return null;
            }
            return type;
        }

        public void Add(UserType type)
        {
            m_typeMap.Add(type.Hash, type);
        }

        public IEnumerator<KeyValuePair<uint, UserType>> GetEnumerator()
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
            var isConst = index.clang_isConstQualifiedType(cxType);
            if (Types.PrimitiveType.TryGetPrimitiveType(cxType, out Types.PrimitiveType primitive))
            {
                return new TypeReference(primitive);
            }

            if (cxType.kind == CXTypeKind._Unexposed)
            {
                // nullptr_t
                return new TypeReference(new PointerType(VoidType.Instance, false));
            }

            if (cxType.kind == CXTypeKind._Pointer)
            {
                return new TypeReference(new PointerType(CxTypeToType(index.clang_getPointeeType(cxType), cursor)));
            }

            if (cxType.kind == CXTypeKind._Typedef)
            {
                // find reference from child cursors
                BaseType type = default;
                ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
                {
                    switch (child.kind)
                    {
                        case CXCursorKind._TypeRef:
                            {
                                var referenced = index.clang_getCursorReferenced(child);
                                type = Get(referenced);
                                if (type is null)
                                {
                                    throw new NotImplementedException();
                                }
                                return CXChildVisitResult._Break;
                            }

                        default:
                            {
                                return CXChildVisitResult._Continue;
                            }
                    }
                });
                if (type is null)
                {
                    var children = cursor.Children();
                    throw new NotImplementedException("Referenced not found");
                }
                else
                {
                    return new TypeReference(type);
                }
            }

            if (cxType.kind == CXTypeKind._Elaborated)
            {
                // typedef struct {} Hoge;
                Types.UserType type = default;
                ClangVisitor.ProcessChildren(cursor, (in CXCursor child) =>
                {
                    switch (child.kind)
                    {
                        case CXCursorKind._StructDecl:
                        case CXCursorKind._UnionDecl:
                            {
                                type = Get(child);
                                if (type is null)
                                {
                                    throw new NotImplementedException();
                                }
                                return CXChildVisitResult._Break;
                            }

                        case CXCursorKind._EnumDecl:
                            {
                                type = Get(child);
                                if (type is null)
                                {
                                    throw new NotImplementedException();
                                }
                                return CXChildVisitResult._Break;
                            }

                        case CXCursorKind._TypeRef:
                            {
                                var referenced = index.clang_getCursorReferenced(child);
                                type = Get(referenced);
                                if (type is null)
                                {
                                    throw new NotImplementedException();
                                }
                                return CXChildVisitResult._Break;
                            }

                        default:
                            return CXChildVisitResult._Continue;
                    }
                });
                if (type is null)
                {
                    var children = cursor.Children();
                    throw new NotImplementedException("Elaborated not found");
                }
                return new TypeReference(type);
            }

            if (cxType.kind == CXTypeKind._FunctionProto)
            {
                var resultType = index.clang_getResultType(cxType);
                return new TypeReference(FunctionType.Parse(cursor, this, resultType));
            }

            throw new NotImplementedException("type not found");
        }
    }
}
