using System;
using System.IO;

namespace ClangCaster
{
    class NamespaceOpener : IDisposable
    {
        System.IO.FileStream m_s;
        public StreamWriter Writer { get; private set; }

        public NamespaceOpener(FileInfo path, string ns, params string[] usingList)
        {
            m_s = new FileStream(path.FullName, FileMode.Create);
            Writer = new StreamWriter(m_s);

            // open namespace
            Writer.WriteLine($"// This source code was generated by ClangCaster");
            foreach(var u in usingList)
            {
                Writer.WriteLine($"using {u};");
            }
            Writer.WriteLine();
            Writer.WriteLine($"namespace {ns}");
            Writer.WriteLine("{");
        }

        public void Dispose()
        {
            // close namespace
            Writer.WriteLine("}");

            Writer.Dispose();
            m_s.Dispose();
        }

        public static NamespaceOpener Open(DirectoryInfo dir, string file, string ns, params string[] usingList)
        {
            dir.Create();
            return new NamespaceOpener(new FileInfo(Path.Combine(dir.FullName, file)), ns, usingList);
        }
    }
}
