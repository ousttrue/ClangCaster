using System;
using System.Collections.Generic;
using System.Linq;
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

        public ExportHeader(string path)
        {
            m_path = new NormalizedFilePath(path);
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
            m_types.Add(type);
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
            var exports = cmd.Headers.Select(x => new ExportHeader(x)).ToArray();

            foreach (var kv in map)
            {
                foreach (var export in exports)
                {
                    if (export.Contains(kv.Value))
                    {
                        export.Push(kv.Value);
                        break;
                    }
                }
            }

            foreach (var export in exports)
            {
                Console.WriteLine(export);
            }
        }
    }
}
