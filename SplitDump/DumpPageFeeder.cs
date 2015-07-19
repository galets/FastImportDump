using System;
using System.Xml;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace SplitDump
{
    public sealed class DumpPageFeeder: IDisposable
    {
        Stream compressedStream;
        Stream input;
        XmlReader xr;
        public int count = 0;
        public int totalCount = 0;

        public DumpPageFeeder(string inputPath)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentException("inputPath");
            }

            if (Path.GetExtension(inputPath) == ".gz")
            {
                compressedStream = File.OpenRead(inputPath);
                input = new GZipStream(compressedStream, CompressionMode.Decompress);
            }
            else
            {
                input = File.OpenRead(inputPath);
            }

            xr = XmlReader.Create(input);
        }

        public DumpPageFeeder(Stream input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
                
            this.input = input;
            xr = XmlReader.Create(input);
        }

        public void Dispose()
        {
            if (xr != null)
            {
                xr.Dispose();
            }

            if (input != null)
            {
                input.Dispose();
            }

            if (compressedStream != null)
            {
                compressedStream.Dispose();
            }
        }

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

        public IEnumerable<XmlDocument> SplitDoc(int batchSize)
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

            while (moreData)
            {
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
                    Console.Error.WriteLine("Unexpected name: {0}. Node will be skipped", xr.Name);
                }
            }

            if (count > 0)
            {
                yield return doc;
            }
        }

    }
}

