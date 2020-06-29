using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClangAggregator;

namespace ClangCaster
{
    class CommandLine
    {
        public List<HeaderWithDll> Headers = new List<HeaderWithDll>();
        public List<string> Includes = new List<string>();
        public List<string> Defines = new List<string>();

        public string Dst;

        public string Namespace;

        public bool DllExportOnly;

        public string TargetFramework = "netstandard2.0";

        public List<string> Using = new List<string>();

        public static CommandLine Parse(string[] args)
        {
            var cmd = new CommandLine();
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-target":
                        cmd.TargetFramework = args[i + 1];
                        ++i;
                        break;

                    case "-using":
                        cmd.Using.Add(args[i + 1]);
                        ++i;
                        break;

                    case "-exportonly":
                        cmd.DllExportOnly = true;
                        break;

                    case "-ns":
                        cmd.Namespace = args[i + 1];
                        ++i;
                        break;

                    case "-out":
                        // dst
                        cmd.Dst = args[i + 1];
                        ++i;
                        break;

                    case "-D":
                        // dst
                        cmd.Defines.Add(args[i + 1]);
                        ++i;
                        break;

                    case "-h":
                        // header
                        {
                            cmd.Headers.Add(new HeaderWithDll(args[i + 1]));
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
            using (var tu = ClangTU.Parse(cmd.Headers.Select(x => x.Header), cmd.Includes, cmd.Defines))
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

            // generate source
            if (!string.IsNullOrEmpty(cmd.Dst))
            {
                // dst folder
                Console.WriteLine($"dst: {cmd.Dst}");
                var dst = new DirectoryInfo(cmd.Dst);
                if (dst.Exists)
                {
                    // clear dst
                    Directory.Delete(dst.FullName, true);
                }
                Directory.CreateDirectory(dst.FullName);

                var exporter = new CSGenerator();
                exporter.Export(map, dst, cmd.Headers, cmd.Namespace, cmd.DllExportOnly, "C", cmd.Using, cmd.TargetFramework);
            }
        }
    }
}
