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
        Dictionary<NormalizedFilePath, ExportSource> m_headerMap = new Dictionary<NormalizedFilePath, ExportSource>();

        public IDictionary<NormalizedFilePath, ExportSource> HeaderMap => m_headerMap;

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
                    Add(type, new UserType[] { });
                    return;
                }
            }
            // skip
        }

        void Add(ClangAggregator.Types.UserType type, ClangAggregator.Types.UserType[] stack)
        {
            if (type is null)
            {
                return;
            }
            if (stack.Contains(type))
            {
                // avoid recursive loop
                return;
            }

            // Add
            if (!m_headerMap.TryGetValue(type.Location.Path, out ExportSource export))
            {
                export = new ExportSource(type.Location.Path);
                m_headerMap.Add(type.Location.Path, export);
            }

            if (string.IsNullOrEmpty(type.Name))
            {
                // 名無し。stack を辿って typedef があればその名前をいただく
                if (stack.Any() && stack.Last() is TypedefType stackTypedef)
                {
                    type.Name = stackTypedef.Name;
                }
                else
                {
                    var a = 0;
                }
            }
            export.Push(type);

            // 依存する型を再帰的にAddする
            if (type is EnumType)
            {
                // end
            }
            else if (type is TypedefType typedefType)
            {
                Add(typedefType.Ref.Type as UserType, stack.Concat(new[] { type }).ToArray());
            }
            else if (type is StructType structType)
            {

            }
            else if (type is FunctionType functionType)
            {

            }
            else
            {
                throw new NotImplementedException();
            }
        }

        const string TEMPLATE = @"
namespace {{ ns }}
{
{% for enum in enums %}
    // {{ enum.Location.Path.Path }}: {{ enum.Location.Line }}
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
            DotLiquid.Template.RegisterSafeType(typeof(EnumType), new string[] { "Hash", "Name", "Location" });
            DotLiquid.Template.RegisterSafeType(typeof(FileLocation), new string[] { "Path", "Line" });
            DotLiquid.Template.RegisterSafeType(typeof(NormalizedFilePath), new string[] { "Path" });

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
