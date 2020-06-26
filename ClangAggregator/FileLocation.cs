using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ClangAggregator
{
    public struct NormalizedFilePath : IEquatable<NormalizedFilePath>
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
        internal static extern int GetLongPathNameW(string src, char[] buffer, int bufferLength);

        const int MAX_PATH = 260;

        static char[] s_buffer = new char[MAX_PATH];

        static Dictionary<string, string> s_pathCache = new Dictionary<string, string>();

        static string Normalize(string path)
        {
            if (s_pathCache.TryGetValue(path, out string value))
            {
                return value;
            }

            var length = GetLongPathNameW(path, s_buffer, s_buffer.Length);
            if (length == 0)
            {
                throw new NotImplementedException();
                // return path;
            }
            value = new String(s_buffer, 0, length);

            // use '/'
            value = System.IO.Path.GetFullPath(value).Replace("\\", "/");

            s_pathCache.Add(path, value);
            return value;
        }

        public string Path { get; private set; }
        readonly int m_hash;
        public NormalizedFilePath(string pathString)
        {
            Path = Normalize(pathString);

            m_hash = Path.ToLower().GetHashCode();
        }

        public override string ToString()
        {
            return Path;
        }

        public bool Equals(NormalizedFilePath other)
        {
            return String.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (obj is NormalizedFilePath)
            {
                return Equals((NormalizedFilePath)obj);
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(NormalizedFilePath lhs, NormalizedFilePath rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(NormalizedFilePath lhs, NormalizedFilePath rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return m_hash;
        }
    }

    public struct FileLocation
    {
        public NormalizedFilePath Path { get; private set; }

        public bool IsValid => !string.IsNullOrEmpty(Path.Path);

        // text position
        public uint Line { get; private set; }
        public readonly uint Column;

        // byte offset
        public readonly int Begin;
        public readonly int End;

        public FileLocation(string path, uint line, uint column, int begin, int end)
        {
            Path = new NormalizedFilePath(path);
            Line = line;
            Column = column;
            Begin = begin;
            End = end;
        }

        public byte[] ReadAllBytes()
        {
            return File.ReadAllBytes(Path.Path);
        }
    }
}
