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
        public StructType AnonymousParent;
        public List<TypeReference> AnonymousTypes;
        List<FunctionType> m_methods;
        public List<FunctionType> Methods
        {
            get
            {
                if (m_methods is null)
                {
                    m_methods = new List<FunctionType>();
                }
                return m_methods;
            }
        }

        public TypeReference BaseClass;
        public Guid IID;

        StructType(string name) : base(name)
        {
            Fields = new List<StructField>();
        }

        public override string ToString()
        {
            var fields = string.Join(", ", Fields.Select(x => x.Ref.Type));
            return $"struct {Name} {{}}";
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

        public static bool TryGetIID(string src, out Guid iid)
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

        public int CalcVTable()
        {
            var index = 0;
            if (BaseClass != null)
            {
                if (BaseClass.Type is StructType baseStruct)
                {
                    index = baseStruct.CalcVTable();
                }
                else if (BaseClass.Type is TypedefType baseTypedef)
                {
                    if (baseTypedef.Ref.Type is StructType baseTypedefStruct)
                    {
                        index = baseTypedefStruct.CalcVTable();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            foreach (var method in Methods)
            {
                method.VTableIndex = index++;
            }
            return index;
        }
    }
}
