﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SourceCombiner
{
    public sealed class SourceCombiner
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Console.WriteLine("You must provide at least 2 arguments. The first is the solution file path and the second is the output file path.");
                return;
            }

            var srcDirectoryPath = args[0];
            var outputFilePath = args[1];

            var openFile = false;
            if (args.Length > 2)
            {
                Boolean.TryParse(args[2], out openFile);
            }

            var filesToParse = Directory.GetFiles(srcDirectoryPath, "*.cs", SearchOption.AllDirectories).ToList();
            var namespaces = GetUniqueNamespaces(filesToParse);

            var assembly = filesToParse.FindAll(fn => fn.Contains("AssemblyInfo.cs")); ;
            if (assembly != null)
                assembly.ForEach(f => filesToParse.Remove(f));

            var outputSource = GenerateCombinedSource(namespaces, filesToParse);

            File.WriteAllText(outputFilePath, outputSource);

            if (openFile)
            {
                Process.Start(outputFilePath);
            }
        }

        private static string GenerateCombinedSource(IEnumerable<string> namespaces, IEnumerable<string> files)
        {
            var filesList = files.ToList();

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"// * File generated by SourceCombiner.exe using {filesList.Count} source files.");
            sb.AppendLine($"// * Created On: {DateTime.Now}");

            foreach (var ns in namespaces.OrderBy(s => s))
            {
                sb.AppendLine("using " + ns + ";");
            }

            foreach (var file in files)
            {
                sb.AppendLine(@"//*** SourceCombiner -> original file " + Path.GetFileName(file) + " ***");

                var openingTag = "using ";
                var sourceLines = File.ReadAllLines(file);
                
                foreach (var sourceLine in sourceLines)
                {
                    var trimmedLine = sourceLine.Trim().Replace("  ", " ");
                    var isUsingDir = trimmedLine.StartsWith(openingTag) && trimmedLine.EndsWith(";");
                    var isAssemblyAttribute = trimmedLine.StartsWith("[assembly:");
                    
                    if (!string.IsNullOrWhiteSpace(sourceLine) && !isUsingDir && !isAssemblyAttribute)
                    {
                        sb.AppendLine(sourceLine);
                    }
                }
            }

            return sb.ToString();
        }

        private static IEnumerable<string> GetUniqueNamespaces(IEnumerable<string> files)
        {
            var names = new List<string>();
            const string openingTag = "using ";
            const int namespaceStartIndex = 6;

            foreach (var file in files)
            {
                var sourceLines = File.ReadAllLines(file);

                foreach (var sourceLine in sourceLines)
                {
                    var trimmedLine = sourceLine.Trim().Replace("  ", " ");
                    if (trimmedLine.StartsWith(openingTag) && trimmedLine.EndsWith(";"))
                    {
                        var name = trimmedLine.Substring(namespaceStartIndex, trimmedLine.Length - namespaceStartIndex - 1);

                        if (!names.Contains(name))
                        {
                            names.Add(name);
                        }
                    }
                }
            }

            return names;
        }
    }
}