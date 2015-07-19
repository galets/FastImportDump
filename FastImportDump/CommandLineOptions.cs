using System;
using CommandLine;
using CommandLine.Text;

namespace FastImportDump
{
    public class CommandLineOptions
    {
        [Option('i', "input", HelpText = "Source xml (uncompressed)", DefaultValue = "")]
        public string sourceXml { get; set; }

        [Option('w', "wmpath", HelpText = "Wikimedia install location", DefaultValue = "/usr/share/webapps/mediawiki")]
        public string wikimediaWorkDir { get; set; }

        [Option('l', "spool", HelpText = "Path where spool batches would be stored", DefaultValue = "/tmp/fast-import")]
        public string spoolPath { get; set; }

        [Option('d', "import-dump", HelpText = "Path where importDump.php is located", DefaultValue = "maintenance/importDump.php")]
        public string importDump { get; set; }

        [Option('p', "parallel", HelpText = "How many parallel processes would be launched", DefaultValue = 64)]
        public int parallelism { get; set; }

        [Option('s', "skip", HelpText = "Skip first X batches", DefaultValue = 0)]
        public int skip  { get; set; }

        [Option('b', "batch-size", HelpText = "How many pages are in a single batch", DefaultValue = 50)]
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

