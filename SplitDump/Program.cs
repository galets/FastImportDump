using System;
using System.IO;
using System.Linq;

namespace SplitDump
{
    class MainClass
    {
        static CommandLineOptions opts = new CommandLineOptions();

        public static int Main(string[] args)
        {
            if (!CommandLine.Parser.Default.ParseArguments(args, opts))
            {
                return 1;
            }

            if (!Directory.Exists(opts.spoolPath))
            {
                Directory.CreateDirectory(opts.spoolPath);
            }

            using (var dpf = string.IsNullOrEmpty(opts.sourceXml) ? new DumpPageFeeder(Console.OpenStandardInput()) : new DumpPageFeeder(opts.sourceXml))
            {
                var position = opts.skip;
                var data = dpf.SplitDoc(opts.batchSize).Skip(opts.skip);

                foreach (var b in data)
                {
                    ++position;
                    var fileName = Path.Combine(opts.spoolPath, string.Format("batch_{0}.xml", position));
                    b.Save(fileName);

                    Console.Write(".");
                }

                Console.WriteLine();
                Console.WriteLine("All completed successfully. Total of {0} articles dumped", dpf.totalCount);
            }

            return 0;
        }
    }
}
