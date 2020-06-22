using System;
using CIndex;

namespace ClangAggregator
{
    public static class TokenExtensions
    {
        public static string Spelling(this in CXToken token, IntPtr tu)
        {
            using (var clangString = ClangString.FromToken(tu, token))
            {
                return clangString.ToString();
            }
        }
    }
}