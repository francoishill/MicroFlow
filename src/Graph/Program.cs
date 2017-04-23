using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MicroFlow.Graph
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: MicroFlow.Graph assembly-file-path output-dir");
                return;
            }

            var assemblyFilePath = args[0];
            var outputDir = args[1];

            try
            {
                Console.WriteLine("Load assembly...");
                var assembly = Assembly.LoadFrom(assemblyFilePath);
                var workflowTypes = assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(Flow)))
                    .ToArray();

                if (!workflowTypes.Any())
                {
                    throw new Exception($"No type found in '{assemblyFilePath}' that implements the Flow type!");
                }

                foreach (var workflowType in workflowTypes)
                {
                    Console.WriteLine($"Type {workflowType.FullName} found");

                    Console.WriteLine("Extract flow description...");
                    var flowDescription = FlowDescriptionExtractor.ExtractFrom(workflowType);

                    Console.WriteLine("Generate graph...");
                    var graph = new FlowGraphBuilder().GenerateDgml(flowDescription);

                    var outputFile = Path.Combine(outputDir, workflowType.FullName.Replace(".", "\\")) + ".dgml";
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                    Console.WriteLine($"Save to file {outputFile}");
                    graph.Save(outputFile);
                }

                Console.WriteLine("Done!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}