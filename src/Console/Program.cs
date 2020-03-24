using System;
using System.Text;
using CommandLine;


namespace AK.EFContextCommon.Console
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
        [Option('c', "context", Required = false, HelpText = "DBContext derived class to be scaffolded")]
        public string ContextClass { get; set; }

        [Option('p', "path", Required = false, HelpText = "Path to assembly to be scaffolded")]
        public string Path { get; set; }

        [Option('o', "output", Required = false, HelpText = "Path to output files")]
        public string Output { get; set; }

        [Option('r', "rewrite", Required = false, HelpText = "Rewrite output files", Default = true)]
        public bool Rewrite { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(o => {
                Char[] charToTrim = {' '};
                string _path = o.Path.Trim(charToTrim);
                string _out = o.Output.Trim(charToTrim);
                string _class = o.ContextClass.Trim(charToTrim);

                Scaffolder _scaffolder = new Scaffolder(_path,_out);
                _scaffolder.ReadAssembly();
            });
            System.Console.WriteLine("Created dll successifuly!");
        }
    }
}
