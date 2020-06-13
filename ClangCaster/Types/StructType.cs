using System;
using libclang;

namespace ClangCaster.Types
{
    public class StructType : UserType
    {
        public bool IsUnion;
        public bool IsForwardDecl;
        public StructType Definition;

        public StructType((uint, ClangLocation, string) args) : base(args)
        { }

        public override string ToString()
        {
            if (IsForwardDecl)
            {
                return $"struct {Name};";
            }
            else
            {
                return $"struct {Name} {{}}";
            }
        }

        // https://joshpeterson.github.io/identifying-a-forward-declaration-with-libclang
        public static bool IsForwardDeclaration(in CXCursor cursor)
        {
            var definition = index.clang_getCursorDefinition(cursor);

            // If the definition is null, then there is no definition in this translation
            // unit, so this cursor must be a forward declaration.
            if (index.clang_equalCursors(definition, index.clang_getNullCursor()))
            {
                return true;
            }

            // If there is a definition, then the forward declaration and the definition
            // are in the same translation unit. This cursor is the forward declaration if
            // it is _not_ the definition.
            return index.clang_equalCursors(cursor, definition);
        }
    }
}
