using System;
using ClangAggregator.Types;

namespace ClangCaster
{
    public abstract class CSType
    {
        protected abstract string WithRef { get; }

        /// <summary>
        /// ClangCaster.Types.BaseType から CSharp の型を表す文字列と属性(struct用)を返す
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public (string, string) ToCSType(BaseType type)
        {
            if (type is PrimitiveType primitiveType)
            {
                switch (primitiveType)
                {
                    case Int8Type int8Type: return ("sbyte", null);
                    case Int16Type int16Type: return ("short", null);
                    case Int32Type int32Type: return ("int", null);
                    case Int64Type int64Type: return ("long", null);
                    case UInt8Type uint8Type: return ("byte", null);
                    case UInt16Type uint16Type: return ("ushort", null);
                    case UInt32Type uint32Type: return ("uint", null);
                    case UInt64Type uint64Type: return ("ulong", null);
                    case Float32Type float32Type: return ("float", null);
                    case Float64Type float64Type: return ("double", null);
                    case VoidType voidType: return ("void", null);
                }

                throw new NotImplementedException();
            }
            if (type is EnumType enumType)
            {
                return (enumType.Name, null);
            }
            if (type is StructType structType)
            {
                return (structType.Name, null);
            }

            if (type is PointerType pointerType)
            {
                if (pointerType.Pointee.Type is PointerType)
                {
                    // double pointer
                    return ($"{WithRef}IntPtr", null);
                }
                else if (pointerType.Pointee.Type is VoidType)
                {
                    return ("IntPtr", null);
                }
                else if (pointerType.Pointee.Type is PrimitiveType primitivePointee)
                {
                    if (string.IsNullOrEmpty(WithRef))
                    {
                        return ("IntPtr", null);
                    }
                    else
                    {
                        var (tmp, _) = ToCSType(primitivePointee);
                        return ($"{WithRef}{tmp}", null);
                    }
                }
                else if (pointerType.Pointee.Type is TypedefType typedefPointee)
                {
                    if (typedefPointee.Ref.Type is PointerType)
                    {
                        return ($"{WithRef}IntPtr", null);
                    }
                    else
                    {
                        return ("IntPtr", null);
                    }
                }
                else
                {
                    return ("IntPtr", null);
                }
            }

            if (type is ArrayType arrayType)
            {
                var elementType = ToCSType(arrayType.Element.Type).Item1;
                return ($"{elementType}[]", $"[MarshalAs(UnmanagedType.ByValArray, SizeConst = {arrayType.Size})]");
            }

            if (type is TypedefType typedefType)
            {
                return ToCSType(typedefType.Ref.Type);
            }

            throw new NotImplementedException();
        }
    }

    class FieldType : CSType
    {
        protected override string WithRef => "";
    }
    class ReturnType : CSType
    {
        protected override string WithRef => "";
    }
    class ParamType : CSType
    {
        protected override string WithRef => "ref ";
    }
}
