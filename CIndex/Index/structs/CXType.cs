// This source code was generated by ClangCaster
using System;
using System.Runtime.InteropServices;

namespace CIndex
{
    // C:/Program Files/LLVM/include/clang-c/Index.h:3398
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CXType // 1
    {
        public CXTypeKind kind;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public IntPtr[] data;
    }
}
