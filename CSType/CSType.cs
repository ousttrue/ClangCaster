using System;
using System.Linq;
using ClangAggregator.Types;

namespace CSType
{
    public enum TypeContext
    {
        Field,
        Param,
        Return,
    }

    static class TypeContextExtensions
    {
        public static string PointerType(this TypeContext context, string src)
        {
            switch (context)
            {
                case TypeContext.Field:
                    return "IntPtr";

                case TypeContext.Return:

                    return "IntPtr";

                case TypeContext.Param:
                    return $"ref {src}";
            }

            throw new NotImplementedException();
        }
    }

    public static class Converter
    {
        /// <summary>
        /// ClangCaster.Types.BaseType から CSharp の型を表す文字列と属性(struct用)を返す
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static (string, string) Convert(TypeContext context, BaseType type)
        {
            // FIXME: もうちょっと場合分けを整理する
            // FIXME: IntPtr でごまかしたところ
            if (type is PrimitiveType primitiveType)
            {
                switch (primitiveType)
                {
                    case BoolType boolType: return ("bool", null);
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
                    return (context.PointerType("IntPtr"), null);
                }
                else if (pointerType.Pointee.Type is VoidType)
                {
                    return ("IntPtr", null);
                }
                else if (pointerType.Pointee.Type is Int8Type)
                {
                    // avoid ref sbyte
                    return (context.PointerType("byte"), null);
                }
                else if (pointerType.Pointee.Type is StructType structPointee && structPointee.Fields.Any())
                {
                    // not forward decl
                    return (context.PointerType(structPointee.Name), null);
                }
                else if (pointerType.Pointee.Type is PrimitiveType primitivePointee)
                {
                    var (tmp, _) = Convert(context, primitivePointee);
                    return (context.PointerType(tmp), null);
                }
                else if (pointerType.Pointee.Type is TypedefType typedefPointee)
                {
                    if (typedefPointee.Ref.Type is PointerType)
                    {
                        return (context.PointerType("IntPtr"), null);
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
                var elementType = Convert(context, arrayType.Element.Type).Item1;
                return ($"{elementType}[]", $"[MarshalAs(UnmanagedType.ByValArray, SizeConst = {arrayType.Size})]");
            }

            if (type is TypedefType typedefType)
            {
                if (typedefType.Ref.Type is PointerType)
                {
                    return ("IntPtr", null);
                }
                return Convert(context, typedefType.Ref.Type);
            }

            throw new NotImplementedException();
        }
    }
}
