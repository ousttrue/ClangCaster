// This source code was generated by ClangCaster
using System;
using System.Runtime.InteropServices;

namespace CIndex {
    [StructLayout(LayoutKind.Sequential)]
    public struct CXPlatformAvailability // 1
    {
        public CXString Platform;
        public CXVersion Introduced;
        public CXVersion Deprecated;
        public CXVersion Obsoleted;
        public int Unavailable;
        public CXString Message;
    }
}