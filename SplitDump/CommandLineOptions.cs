using System;
using CommandLine;
using CommandLine.Text;

namespace SplitDump
{
    public class CommandLineOptions
    {
        [Option('i', "input", HelpText = "Source xml file (uncompressed or gzip)", DefaultValue = "")]
        public string sourceXml { get; set; }

        [Option('l', "spool", HelpText = "Path where spool batches would be stored", Required = true)]
        public string spoolPath { get; set; }

        [Option('s', "skip", HelpText = "Skip first X batches", DefaultValue = 0)]
        public int skip  { get; set; }

        [Option('b', "batch-size", HelpText = "How many pages are in a single batch", DefaultValue = 1000)]
        public int batchSize  { get; set; }

        [HelpOption]
        public string GetUsage() 
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current)
            );
        }
    }
}

