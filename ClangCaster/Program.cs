using System;
using System.Collections.Generic;

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

    class Program
    {
        static void Main(string[] args)
        {
            var cmd = CommandLine.Parse(args);
            using (var tu = ClangTU.Parse(cmd.Headers, cmd.Includes, cmd.Defines))
            {
                if(tu is null)
                {
                    Console.WriteLine("fail to parse");
                    return;
                }
                Console.WriteLine($"Parsed: {tu}");
            }
        }
    }
}
