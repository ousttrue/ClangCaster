// This source code was generated by ClangCaster
using System;
using System.Runtime.InteropServices;

namespace CIndex
{

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
