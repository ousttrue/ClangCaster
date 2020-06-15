using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClangAggregator;
using ClangAggregator.Types;

namespace ClangCaster
{
    class Exporter
    {
        List<NormalizedFilePath> m_rootHeaders;
        Dictionary<NormalizedFilePath, ExportHeader> m_headerMap = new Dictionary<NormalizedFilePath, ExportHeader>();

        public IDictionary<NormalizedFilePath, ExportHeader> HeaderMap => m_headerMap;

        public Exporter(IEnumerable<string> headers)
        {
            m_rootHeaders = headers.Select(x => new NormalizedFilePath(x)).ToList();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var kv in m_headerMap)
            {
                sb.AppendLine(kv.Value.ToString());
            }
            return sb.ToString();
        }

        public void Push(ClangAggregator.Types.UserType type)
        {
            foreach (var root in m_rootHeaders)
            {
                if (root.Equals(type.Location.Path))
                {
                    Add(type);
                    return;
                }
            }
            // skip
        }

        void Add(ClangAggregator.Types.UserType type)
        {
            if (!m_headerMap.TryGetValue(type.Location.Path, out ExportHeader export))
            {
                export = new ExportHeader(type.Location.Path);
                m_headerMap.Add(type.Location.Path, export);
            }

            export.Push(type);

            // 依存する型を再帰的にAddする

            // typedef
            // struct
            // function
        }

        const string TEMPLATE = @"
namespace {{ ns }}
{
{% for enum in enums %}
    enum {{ enum.Name }}
    {

    }
{% endfor %}
}
";

        static string SourcePath(DirectoryInfo directory, NormalizedFilePath f)
        {
            var stem = Path.GetFileNameWithoutExtension(f.Path);
            return Path.Combine(directory.FullName, $"{stem}.cs");
        }

        public void Export(DirectoryInfo dst)
        {
            DotLiquid.Template.RegisterSafeType(typeof(EnumType), new string[] { "Name" });
            var template = DotLiquid.Template.Parse(TEMPLATE);
            foreach (var kv in HeaderMap)
            {
                var path = SourcePath(dst, kv.Key);
                Console.WriteLine(path);
                using (var s = new FileStream(path, FileMode.Create))
                using (var w = new StreamWriter(s))
                {
                    // DotLiquid
                    var rendered = template.Render(DotLiquid.Hash.FromAnonymousObject(
                        new
                        {
                            ns = "clang",
                            enums = kv.Value.EnumTypes,
                        }
                    ));
                    w.Write(rendered);
                }
            }
        }
    }
}
