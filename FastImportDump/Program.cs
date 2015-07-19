using System;
using System.Linq;
using System.Xml;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FastImportDump
{
    class MainClass
    {
        static CommandLineOptions opts = new CommandLineOptions();

        static string sourceXml { get { return opts.sourceXml; } }
        static string wikimediaWorkDir { get { return opts.wikimediaWorkDir; } }
        static string spoolPath { get { return opts.spoolPath; } }
        static string importDump { get { return opts.importDump; } }
        static int parallelism { get { return opts.parallelism; } }
        static int skip { get { return opts.skip; } }
        static int batchSize { get { return opts.batchSize; } }

        static volatile bool exitFlag = false;


        static bool SkipToNode(XmlReader xr)
        {
            do
            {
                if (!xr.Read())
                {
                    return false;
                }
            } while(xr.NodeType != XmlNodeType.Element);

            return true;
        }

        static IEnumerable<XmlDocument> SplitDoc(XmlReader xr)
        {
            xr.MoveToContent();

            var xdocTemplate = new XmlDocument(xr.NameTable);
            var rootElement = (XmlElement)xdocTemplate.AppendChild(xdocTemplate.CreateElement(xr.Name, xr.NamespaceURI));
            for(bool success = xr.MoveToFirstAttribute(); success; success = xr.MoveToNextAttribute())
            {
                rootElement.Attributes.Append(xdocTemplate.CreateAttribute(xr.Name, xr.NamespaceURI)).Value = xr.Value;
            }

            bool moreData = SkipToNode(xr);

            if (moreData && xr.Name == "siteinfo")
            {
                var siteInfoNode = xdocTemplate.ReadNode(xr);
                rootElement.AppendChild(siteInfoNode);
                moreData = SkipToNode(xr);
            }

            var doc = new XmlDocument(xdocTemplate.NameTable);
            doc.AppendChild(doc.ImportNode(xdocTemplate.FirstChild, true));
            var count = 0;
            var totalCount = 0;

            while (moreData)
            {
                if (exitFlag)
                {
                    yield break;
                }

                if (xr.Name == "page")
                {
                    var page = doc.ReadNode(xr);
                    doc.FirstChild.AppendChild(page);
                    moreData = SkipToNode(xr);

                    ++totalCount;
                    ++count;
                    if (count >= batchSize)
                    {
                        var doc2 = doc;
                        doc = new XmlDocument(xdocTemplate.NameTable);
                        doc.AppendChild(doc.ImportNode(xdocTemplate.FirstChild, true));
                        count = 0;
                        yield return doc2;
                    }
                }
                else
                {
                    Console.WriteLine("wtf, name is {0}", xr.Name);
                }
            }

            if (count > 0)
            {
                Console.WriteLine("Finished, total {0} pages", totalCount);
                yield return doc;
            }
        }

        static void DoImport(XmlDocument xdoc, int number)
        {
            Console.WriteLine("Importing batch #{0}", number);

            var psi = new ProcessStartInfo()
            {
                FileName = "php",
                Arguments = importDump,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = wikimediaWorkDir,
                UseShellExecute = false,

            };

            var process = Process.Start(psi);
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => { };
            process.BeginOutputReadLine();
            xdoc.Save(process.StandardInput);
            process.StandardInput.Close();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var p = Path.Combine(spoolPath, "batch_" + number + ".xml");

                Console.WriteLine("Error importing batch {0}, xml will be saved to {1}", number, p);
                if (!Directory.Exists(spoolPath))
                {
                    Directory.CreateDirectory(spoolPath);
                }

                xdoc.Save(p);
            }
        }


        public static int Main(string[] args)
        {
            if (!CommandLine.Parser.Default.ParseArguments(args, opts))
            {
                return 1;
            }

            var exitMonitorTask = Task.Run(() =>
            {
                Console.WriteLine("Starting to read records. Press Enter to shutdown.");
                Console.ReadLine();
                Console.WriteLine("Exit requested from console. Please wait for processes to exit.");
                exitFlag = true;
            });

            using (var xr = string.IsNullOrEmpty(sourceXml) ? XmlReader.Create(Console.OpenStandardInput()) : XmlReader.Create(sourceXml))
            {

                int count = skip;
                SplitDoc(xr)
                    .Skip(skip)
                    .AsParallel()
                    .WithDegreeOfParallelism(parallelism)
                    .ForAll(d =>
                {
                    var count2 = Interlocked.Increment(ref count);
                    DoImport(d, count2);
                });
                
                Console.WriteLine("Exiting. Last batch was {0}.Total pages: {1}", count, count * batchSize);
            }

            return exitFlag ? 2 : 0;
        }
    }
}
