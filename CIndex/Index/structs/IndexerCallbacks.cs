// This source code was generated by ClangCaster
using System;
using System.Runtime.InteropServices;

namespace CIndex {
    [StructLayout(LayoutKind.Sequential)]
    public struct IndexerCallbacks // 1
    {
        public IntPtr abortQuery;
        public IntPtr diagnostic;
        public IntPtr enteredMainFile;
        public IntPtr ppIncludedFile;
        public IntPtr importedASTFile;
        public IntPtr startedTranslationUnit;
        public IntPtr indexDeclaration;
        public IntPtr indexEntityReference;
    }
}
