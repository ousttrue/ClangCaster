using System;

namespace ClangAggregator
{
    public struct NormalizedFilePath : IEquatable<NormalizedFilePath>
    {
        public readonly string Path;
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
        public readonly NormalizedFilePath Path;

        // text position
        public readonly uint Line;
        public readonly uint Column;

        // byte offset
        public readonly uint Begin;
        public readonly uint End;

        public FileLocation(string path, uint line, uint column, uint begin, uint end)
        {
            Path = new NormalizedFilePath(path);
            Line = line;
            Column = column;
            Begin = begin;
            End = end;
        }
    }
}
