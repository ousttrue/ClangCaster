using libclang;

namespace ClangAggregator.Types
{
    public class VoidType : PrimitiveType
    {
        VoidType() : base("void")
        { }

        public static VoidType Instance = new VoidType();
    }

    public class BoolType : PrimitiveType
    {
        BoolType() : base("bool")
        { }

        public static BoolType Instance = new BoolType();
    }

    public class Int8Type : PrimitiveType
    {
        Int8Type() : base("int8")
        { }

        public static Int8Type Instance = new Int8Type();
    }

    public class Int16Type : PrimitiveType
    {
        Int16Type() : base("int16")
        { }

        public static Int16Type Instance = new Int16Type();
    }

    public class Int32Type : PrimitiveType
    {
        Int32Type() : base("int32")
        { }

        public static Int32Type Instance = new Int32Type();
    }

    public class Int64Type : PrimitiveType
    {
        Int64Type() : base("int64")
        { }

        public static Int64Type Instance = new Int64Type();
    }

    public class UInt8Type : PrimitiveType
    {
        UInt8Type() : base("uint8")
        { }

        public static UInt8Type Instance = new UInt8Type();
    }

    public class UInt16Type : PrimitiveType
    {
        UInt16Type() : base("uint16")
        { }

        public static UInt16Type Instance = new UInt16Type();
    }

    public class UInt32Type : PrimitiveType
    {
        UInt32Type() : base("uint32")
        { }

        public static UInt32Type Instance = new UInt32Type();
    }

    public class UInt64Type : PrimitiveType
    {
        UInt64Type() : base("uint64")
        { }

        public static UInt64Type Instance = new UInt64Type();
    }

    public class Float32Type : PrimitiveType
    {
        Float32Type() : base("float32")
        { }

        public static Float32Type Instance = new Float32Type();
    }

    public class Float64Type : PrimitiveType
    {
        Float64Type() : base("float64")
        { }

        public static Float64Type Instance = new Float64Type();
    }

    public class Float80Type : PrimitiveType
    {
        Float80Type() : base("float80")
        { }

        public static Float80Type Instance = new Float80Type();
    }

    public class PrimitiveType : BaseType
    {
        protected PrimitiveType(string name) : base(name)
        {
        }

        public override string ToString()
        {
            return Name;
        }

        public static bool TryGetPrimitiveType(in CXType src, out PrimitiveType dst)
        {
            // http://clang-developers.42468.n3.nabble.com/llibclang-CXTypeKind-char-types-td3754411.html
            switch (src.kind)
            {
                case CXTypeKind._Void: dst = VoidType.Instance; return true;
                case CXTypeKind._Bool: dst = BoolType.Instance; return true;

                // Int
                case CXTypeKind._Char_S:
                case CXTypeKind._SChar:
                    dst = Int8Type.Instance;
                    return true;
                case CXTypeKind._Short:
                    dst = Int16Type.Instance;
                    return true;
                case CXTypeKind._Int:
                case CXTypeKind._Long:
                    dst = Int32Type.Instance;
                    return true;
                case CXTypeKind._LongLong:
                    dst = Int64Type.Instance;
                    return true;

                // UInt
                case CXTypeKind._Char_U:
                case CXTypeKind._UChar:
                    dst = UInt8Type.Instance;
                    return true;
                case CXTypeKind._UShort:
                case CXTypeKind._WChar:
                    dst = UInt16Type.Instance;
                    return true;
                case CXTypeKind._UInt:
                case CXTypeKind._ULong:
                    dst = UInt32Type.Instance;
                    return true;
                case CXTypeKind._ULongLong:
                    dst = UInt64Type.Instance;
                    return true;

                // Float
                case CXTypeKind._Float:
                    dst = Float32Type.Instance;
                    return true;
                case CXTypeKind._Double:
                    dst = Float64Type.Instance;
                    return true;
                case CXTypeKind._LongDouble:
                    dst = Float80Type.Instance;
                    return true;
            }

            // not found
            dst = null;
            return false;
        }
    }
}
