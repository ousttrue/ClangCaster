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
        static bool TryGetPrimitiveType(BaseType type, out string value)
        {
            if (type is PrimitiveType primitiveType)
            {
                switch (primitiveType)
                {
                    case BoolType boolType: value = "bool"; return true; ;
                    case Int8Type int8Type: value = "sbyte"; return true; ;
                    case Int16Type int16Type: value = "short"; return true; ;
                    case Int32Type int32Type: value = "int"; return true; ;
                    case Int64Type int64Type: value = "long"; return true; ;
                    case UInt8Type uint8Type: value = "byte"; return true; ;
                    case UInt16Type uint16Type: value = "ushort"; return true; ;
                    case UInt32Type uint32Type: value = "uint"; return true; ;
                    case UInt64Type uint64Type: value = "ulong"; return true; ;
                    case Float32Type float32Type: value = "float"; return true; ;
                    case Float64Type float64Type: value = "double"; return true; ;
                    case VoidType voidType: value = "void"; return true; ;
                }
            }

            value = null;
            return false;
        }

        static bool TryGetString(TypeContext context, BaseType baseType, out (string, string) value)
        {
            if (baseType.Name == "LPCSTR")
            {
                switch (context)
                {
                    case TypeContext.Field:
                    case TypeContext.Param:
                        value = ("string", "[MarshalAs(UnmanagedType.LPStr)]");
                        return true;
                }
            }
            else if (baseType.Name == "LPCWSTR")
            {
                switch (context)
                {
                    case TypeContext.Field:
                    case TypeContext.Param:
                        value = ("string", "[MarshalAs(UnmanagedType.LPWStr)]");
                        return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// ClangCaster.Types.BaseType から CSharp の型を表す文字列と属性(struct用)を返す
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static (string, string) Convert(TypeContext context, TypeReference reference)
        {
            var type = reference.Type;
            if (TryGetPrimitiveType(type, out string primitiveType))
            {
                return (primitiveType, null);
            }

            if (TryGetString(context, type, out (string, string) stringTypeWithAttribute))
            {
                return stringTypeWithAttribute;
            }

            if (type is EnumType enumType)
            {
                return (enumType.Name, null);
            }

            if (type is StructType structType)
            {
                return (structType.Name, null);
            }

            if (type is ArrayType arrayType)
            {
                var elementType = Convert(context, arrayType.Element).Item1;
                return ($"{elementType}[]", $"[MarshalAs(UnmanagedType.ByValArray, SizeConst = {arrayType.Size})]");
            }

            if (type is TypedefType typedefType)
            {
                var (name, functionType) = reference.GetFunctionTypeFromTypedef();
                if (functionType != null)
                {
                    // function pointer as delegate
                    return (name, null);
                }
                return Convert(context, typedefType.Ref);
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
                    // void* is always IntPtr
                    return ("IntPtr", null);
                }
                else
                {
                    if (pointerType.Pointee.Type is Int8Type)
                    {
                        // avoid ref sbyte
                        return (context.PointerType("byte"), null);
                    }
                    else if (pointerType.Pointee.Type is StructType structPointee)
                    {
                        if (structPointee.Fields.Any())
                        {
                            return (context.PointerType(structPointee.Name), null);
                        }
                        else
                        {
                            // forward decl
                            return ("IntPtr", null);
                        }
                    }
                    else if (pointerType.Pointee.Type is TypedefType typedefPointee)
                    {
                        if (typedefPointee.Ref.Type is PointerType)
                        {
                            return (context.PointerType("IntPtr"), null);
                        }
                        else if (typedefPointee.Ref.Type is UserType userType)
                        {
                            return (context.PointerType(userType.Name), null);
                        }
                        else
                        {
                            return ("IntPtr", null);
                        }
                    }
                    else if (pointerType.Pointee.Type is FunctionType)
                    {
                        return ("IntPtr", null);
                    }
                    else
                    {
                        var (tmp, _) = Convert(context, pointerType.Pointee);
                        return (context.PointerType(tmp), null);
                    }
                }
            }

            throw new NotImplementedException();
        }
    }
}
