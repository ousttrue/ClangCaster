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
            if (type is TypedefType typedefType)
            {
                type = typedefType.Ref.Type;
            }
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

        static bool TryGetGuid(TypeContext context, BaseType baseType, out (string, string) value)
        {
            if (context == TypeContext.Param)
            {
                if (baseType is PointerType pointerType)
                {
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
            if (type is PointerType pointerType)
            {
                return ConvertPointer(context, pointerType);
            }
            if (type is ArrayType arrayType)
            {
                var elementType = Convert(context, arrayType.Element).Item1;
                if (context == TypeContext.Field && arrayType.Size > 0)
                {
                    if (arrayType.Element.Type.Name == "WCHAR")
                    {
                        return ("string", $"[MarshalAs(UnmanagedType.ByValTStr, SizeConst = {arrayType.Size})]");
                    }
                    return ($"{elementType}[]", $"[MarshalAs(UnmanagedType.ByValArray, SizeConst = {arrayType.Size})]");
                }
                else
                {
                    return ($"ref {elementType}", null);
                }
            }
            else
            {
                return ConvertRaw(context, type);
            }
        }

        public static (string, string) ConvertRaw(TypeContext context, BaseType type)
        {
            if (TryGetPrimitiveType(type, out string primitiveType))
            {
                return (primitiveType, null);
            }

            if (TryGetString(context, type, out (string, string) stringTypeWithAttribute))
            {
                return stringTypeWithAttribute;
            }

            if (TryGetGuid(context, type, out (string, string) guidTypeWithAttribute))
            {
                return guidTypeWithAttribute;
            }

            if (type is EnumType enumType)
            {
                return (enumType.Name, null);
            }

            if (type is StructType structType)
            {
                return (structType.Name, null);
            }

            if (type is TypedefType typedefType)
            {
                var (name, functionType) = type.GetFunctionTypeFromTypedef();
                if (functionType != null)
                {
                    // function pointer as delegate
                    return (name, null);
                }
                return Convert(context, typedefType.Ref);
            }

            throw new NotImplementedException();
        }

        public static (string, string) ConvertPointer(TypeContext context, PointerType pointerType)
        {
            if (TryGetPrimitiveType(pointerType.Pointee.Type, out string primitiveType))
            {
                if (primitiveType == "void")
                {
                    // void* is always IntPtr
                    return ("IntPtr", null);
                }
                return (context.PointerType(primitiveType), null);
            }
            if (pointerType.Pointee.Type is PointerType)
            {
                // double pointer
                return (context.PointerType("IntPtr"), null);
            }
            if (pointerType.Pointee.Type is Int8Type)
            {
                // avoid ref sbyte
                return (context.PointerType("byte"), null);
            }
            if (pointerType.Pointee.Type is TypedefType pointeeType)
            {
                if (pointeeType.Name == "IID")
                {
                    return (context.PointerType("Guid"), null);
                }
            }
            if (pointerType.Pointee.Type is UserType userType)
            {
                if (pointerType.Pointee.Type.Name == "CXToken")
                {
                    return ("IntPtr", null);
                }
            }
            if (pointerType.Pointee.Type is StructType structPointee)
            {
                if (structPointee.IID != default || structPointee.Name == "ID3DInclude")
                {
                    // COM interface
                    return ("IntPtr", null);
                }

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
                else if (typedefPointee.Ref.Type is StructType st)
                {
                    if (st.IID != default || st.Name == "ID3DInclude")
                    {
                        // COM interface
                        return ("IntPtr", null);
                    }
                    return (context.PointerType(st.Name), null);
                }
                else if (typedefPointee.Ref.Type is EnumType et)
                {
                    return (context.PointerType(et.Name), null);
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
}
