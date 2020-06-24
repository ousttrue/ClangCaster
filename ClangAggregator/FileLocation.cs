using System;
using System.IO;

namespace ClangAggregator
{
    public struct NormalizedFilePath : IEquatable<NormalizedFilePath>
    {
        public string Path { get; private set; }
        readonly int m_hash;
        public NormalizedFilePath(string pathString)
        {
            Path = System.IO.Path.GetFullPath(pathString).Replace("\\", "/");
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
