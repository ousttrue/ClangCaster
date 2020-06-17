using System;
using System.Runtime.InteropServices;
using CIndex;

namespace ClangAggregator
{
    static class ClangVisitor
    {
        public delegate CXChildVisitResult CallbackFunc(in CXCursor cursor);

        static CXChildVisitResult Visitor(CXCursor cursor, CXCursor parent, IntPtr data)
        {
            var callback = Marshal.GetDelegateForFunctionPointer<CallbackFunc>(data);
            return callback(cursor);
        }

        delegate CXChildVisitResult VisitorFunc(CXCursor cursor, CXCursor parent, IntPtr data);
        static VisitorFunc s_func = new VisitorFunc(Visitor);
        static IntPtr s_visitorPtr;
        static IntPtr VisitorPtr
        {
            get{
                if(s_visitorPtr==IntPtr.Zero)
                {
                    s_visitorPtr = Marshal.GetFunctionPointerForDelegate(s_func);
                }
                return s_visitorPtr;                
            }
        }

        public static void ProcessChildren(in CXCursor cursor, CallbackFunc callback)
        {
            var p = Marshal.GetFunctionPointerForDelegate(callback);            
            libclang.clang_visitChildren(cursor, VisitorPtr, p);
        }
    }
}
