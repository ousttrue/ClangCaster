// This source code was generated by ClangCaster

namespace CIndex
{
    // C:/Program Files/LLVM/include/clang-c/Index.h:3220
    public enum CXTypeKind // 2
    {
        _Invalid = 0x0,
        _Unexposed = 0x1,
        _Void = 0x2,
        _Bool = 0x3,
        _Char_U = 0x4,
        _UChar = 0x5,
        _Char16 = 0x6,
        _Char32 = 0x7,
        _UShort = 0x8,
        _UInt = 0x9,
        _ULong = 0xa,
        _ULongLong = 0xb,
        _UInt128 = 0xc,
        _Char_S = 0xd,
        _SChar = 0xe,
        _WChar = 0xf,
        _Short = 0x10,
        _Int = 0x11,
        _Long = 0x12,
        _LongLong = 0x13,
        _Int128 = 0x14,
        _Float = 0x15,
        _Double = 0x16,
        _LongDouble = 0x17,
        _NullPtr = 0x18,
        _Overload = 0x19,
        _Dependent = 0x1a,
        _ObjCId = 0x1b,
        _ObjCClass = 0x1c,
        _ObjCSel = 0x1d,
        _Float128 = 0x1e,
        _Half = 0x1f,
        _Float16 = 0x20,
        _ShortAccum = 0x21,
        _Accum = 0x22,
        _LongAccum = 0x23,
        _UShortAccum = 0x24,
        _UAccum = 0x25,
        _ULongAccum = 0x26,
        _FirstBuiltin = 0x2,
        _LastBuiltin = 0x26,
        _Complex = 0x64,
        _Pointer = 0x65,
        _BlockPointer = 0x66,
        _LValueReference = 0x67,
        _RValueReference = 0x68,
        _Record = 0x69,
        _Enum = 0x6a,
        _Typedef = 0x6b,
        _ObjCInterface = 0x6c,
        _ObjCObjectPointer = 0x6d,
        _FunctionNoProto = 0x6e,
        _FunctionProto = 0x6f,
        _ConstantArray = 0x70,
        _Vector = 0x71,
        _IncompleteArray = 0x72,
        _VariableArray = 0x73,
        _DependentSizedArray = 0x74,
        _MemberPointer = 0x75,
        _Auto = 0x76,
        _Elaborated = 0x77,
        _Pipe = 0x78,
        _OCLImage1dRO = 0x79,
        _OCLImage1dArrayRO = 0x7a,
        _OCLImage1dBufferRO = 0x7b,
        _OCLImage2dRO = 0x7c,
        _OCLImage2dArrayRO = 0x7d,
        _OCLImage2dDepthRO = 0x7e,
        _OCLImage2dArrayDepthRO = 0x7f,
        _OCLImage2dMSAARO = 0x80,
        _OCLImage2dArrayMSAARO = 0x81,
        _OCLImage2dMSAADepthRO = 0x82,
        _OCLImage2dArrayMSAADepthRO = 0x83,
        _OCLImage3dRO = 0x84,
        _OCLImage1dWO = 0x85,
        _OCLImage1dArrayWO = 0x86,
        _OCLImage1dBufferWO = 0x87,
        _OCLImage2dWO = 0x88,
        _OCLImage2dArrayWO = 0x89,
        _OCLImage2dDepthWO = 0x8a,
        _OCLImage2dArrayDepthWO = 0x8b,
        _OCLImage2dMSAAWO = 0x8c,
        _OCLImage2dArrayMSAAWO = 0x8d,
        _OCLImage2dMSAADepthWO = 0x8e,
        _OCLImage2dArrayMSAADepthWO = 0x8f,
        _OCLImage3dWO = 0x90,
        _OCLImage1dRW = 0x91,
        _OCLImage1dArrayRW = 0x92,
        _OCLImage1dBufferRW = 0x93,
        _OCLImage2dRW = 0x94,
        _OCLImage2dArrayRW = 0x95,
        _OCLImage2dDepthRW = 0x96,
        _OCLImage2dArrayDepthRW = 0x97,
        _OCLImage2dMSAARW = 0x98,
        _OCLImage2dArrayMSAARW = 0x99,
        _OCLImage2dMSAADepthRW = 0x9a,
        _OCLImage2dArrayMSAADepthRW = 0x9b,
        _OCLImage3dRW = 0x9c,
        _OCLSampler = 0x9d,
        _OCLEvent = 0x9e,
        _OCLQueue = 0x9f,
        _OCLReserveID = 0xa0,
        _ObjCObject = 0xa1,
        _ObjCTypeParam = 0xa2,
        _Attributed = 0xa3,
        _OCLIntelSubgroupAVCMcePayload = 0xa4,
        _OCLIntelSubgroupAVCImePayload = 0xa5,
        _OCLIntelSubgroupAVCRefPayload = 0xa6,
        _OCLIntelSubgroupAVCSicPayload = 0xa7,
        _OCLIntelSubgroupAVCMceResult = 0xa8,
        _OCLIntelSubgroupAVCImeResult = 0xa9,
        _OCLIntelSubgroupAVCRefResult = 0xaa,
        _OCLIntelSubgroupAVCSicResult = 0xab,
        _OCLIntelSubgroupAVCImeResultSingleRefStreamout = 0xac,
        _OCLIntelSubgroupAVCImeResultDualRefStreamout = 0xad,
        _OCLIntelSubgroupAVCImeSingleRefStreamin = 0xae,
        _OCLIntelSubgroupAVCImeDualRefStreamin = 0xaf,
        _ExtVector = 0xb0,
    }
}
