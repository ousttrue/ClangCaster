using System;
using System.Collections.Generic;
using System.IO;
using ClangAggregator;

namespace ClangCaster
{
    class CommandLine
    {
        public List<string> Headers = new List<string>();
        public List<string> Includes = new List<string>();
        public List<string> Defines = new List<string>();

        public string Dst;

        public static CommandLine Parse(string[] args)
        {
            var cmd = new CommandLine();
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-d":
                        // dst
                        cmd.Dst = args[i + 1];
                        ++i;
                        break;

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

            // organize types
            foreach (var kv in map)
            {
                exporter.Push(kv.Value);
            }

            // generate source
            if (!string.IsNullOrEmpty(cmd.Dst))
            {
                Console.WriteLine(cmd.Dst);
                var dst = new DirectoryInfo(cmd.Dst);
                if (dst.Exists)
                {
                    // clear dst
                    Directory.Delete(dst.FullName, true);
                }
                Directory.CreateDirectory(dst.FullName);

                exporter.Export(dst);
            }
        }
    }
}
