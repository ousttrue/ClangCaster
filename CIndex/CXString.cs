// This source code was generated by regenerator"
using System;
using System.Runtime.InteropServices;

namespace CIndex
{

    [StructLayout(LayoutKind.Sequential)]
    public struct CXString // 1
    {
        public IntPtr data;
        public uint private_flags;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct CXStringSet // 1
    {
        public IntPtr Strings;
        public uint Count;
    }

    public static partial class libclang
    {
        [DllImport("libclang.dll")]
        public static extern IntPtr clang_getCString(
            CXString _string
        );
        [DllImport("libclang.dll")]
        public static extern void clang_disposeString(
            CXString _string
        );
        [DllImport("libclang.dll")]
        public static extern void clang_disposeStringSet(
            IntPtr set
        );
    }
}