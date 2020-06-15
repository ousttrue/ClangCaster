using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClangAggregator;

namespace ClangCaster
{
    class CommandLine
    {
        public List<string> Headers = new List<string>();
        public List<string> Includes = new List<string>();
        public List<string> Defines = new List<string>();

        public static CommandLine Parse(string[] args)
        {
            var cmd = new CommandLine();
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-h":
                        // header
                        {
                            cmd.Headers.Add(args[i + 1]);
                            ++i;
                        }
                        break;

                    case "-I":
                        // include directory
                        {
                            cmd.Includes.Add(args[i + 1]);
                            ++i;
                        }
                        break;

                    default:
                        throw new ArgumentException();
                }
            }
            return cmd;
        }
    }

    class ExportHeader
    {
        readonly NormalizedFilePath m_path;
        readonly List<ClangAggregator.Types.UserType> m_types = new List<ClangAggregator.Types.UserType>();

        public ExportHeader(NormalizedFilePath path)
        {
            m_path = path;
        }

        public ExportHeader(string path) : this(new NormalizedFilePath(path))
        {
        }

        public override string ToString()
        {
            return $"{m_path} ({m_types.Count}types)";
        }

        public bool Contains(ClangAggregator.Types.UserType type)
        {
            return m_path.Equals(type.Location.Path);
        }

        public void Push(ClangAggregator.Types.UserType type)
        {
            if (m_types.Contains(type))
            {
                return;
            }
            m_types.Add(type);
        }
    }

    class Exporter
    {
        List<NormalizedFilePath> m_rootHeaders;

        Dictionary<NormalizedFilePath, ExportHeader> m_headerMap = new Dictionary<NormalizedFilePath, ExportHeader>();

        public Exporter(IEnumerable<string> headers)
        {
            m_rootHeaders = headers.Select(x => new NormalizedFilePath(x)).ToList();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach(var kv in m_headerMap)
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
    }

    class Program
    {
        static TypeMap Parse(in CommandLine cmd)
        {
            using (var tu = ClangTU.Parse(cmd.Headers, cmd.Includes, cmd.Defines))
            {
                if (tu is null)
                {
                    Console.WriteLine("fail to parse");
                    return null;
                }

                var aggregator = new TypeAggregator();
                var map = aggregator.Process(tu.GetCursor());
                return map;
            }
        }

        static void Main(string[] args)
        {
            var cmd = CommandLine.Parse(args);
            var map = Parse(cmd);
            var exporter = new Exporter(cmd.Headers);

            foreach (var kv in map)
            {
                exporter.Push(kv.Value);
            }

            Console.WriteLine(exporter);
        }
    }
}
