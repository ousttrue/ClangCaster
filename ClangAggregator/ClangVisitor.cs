using System;
using System.Runtime.InteropServices;
using libclang;

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
        static IntPtr s_visitorPtr;
        static IntPtr VisitorPtr
        {
            get{
                if(s_visitorPtr==IntPtr.Zero)
                {
                    s_visitorPtr = Marshal.GetFunctionPointerForDelegate(new VisitorFunc(Visitor));
                }
                return s_visitorPtr;                
            }
        }

        public static void ProcessChildren(in CXCursor cursor, CallbackFunc callback)
        {
            var p = Marshal.GetFunctionPointerForDelegate(callback);            
            index.clang_visitChildren(cursor, VisitorPtr, p);
        }
    }
}
