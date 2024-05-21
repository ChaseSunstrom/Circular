
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Circular
{
    public class DependencyChecker
    {
        public void CheckDependencies(string directory)
        {
            var circularDeps = FindCircularDependencies(directory);

            if (circularDeps.Any())
            {
                throw new InvalidOperationException($"Circular dependencies detected:\n{string.Join("\n", circularDeps.Select(c => string.Join(" -> ", c)))}");
            }
        }

        private List<List<string>> FindCircularDependencies(string directory)
        {
            var graph = BuildDependencyGraph(directory);
            return DetectCycles(graph);
        }

        private Dictionary<string, List<string>> BuildDependencyGraph(string directory)
        {
            var graph = new Dictionary<string, List<string>>();
            var ignoredPaths = ReadIgnoredPaths(directory);

            foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Select(f => Path.GetFullPath(f))
                .Where(f => f.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".c", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".cc", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".h", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".hpp", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".hh", StringComparison.OrdinalIgnoreCase))
                .Where(f => !ignoredPaths.Contains(f)))
            {
                var includes = FindIncludes(file);
                graph[file] = includes.Where(i => !ignoredPaths.Contains(Path.GetFullPath(i))).ToList();
            }
            return graph;
        }

        private List<string> FindIncludes(string filePath)
        {
            var includes = new List<string>();
            var content = File.ReadAllText(filePath);
            var includeMatches = Regex.Matches(content, @"#include\s*[""<](.*?)[>""]");

            foreach (Match match in includeMatches)
            {
                includes.Add(NormalizePath(filePath, match.Groups[1].Value));
            }

            return includes;
        }

        private string NormalizePath(string filePath, string includePath)
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filePath), includePath));
        }
        private HashSet<string> ReadIgnoredPaths(string directory)
        {
            var ignoredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ignoreFilePath = Path.Combine(directory, "CircularIgnore.json");

            if (File.Exists(ignoreFilePath))
            {
                var ignoreFile = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(ignoreFilePath));
                if (ignoreFile != null)
                {
                    foreach (var path in ignoreFile)
                    {
                        var fullPath = Path.GetFullPath(Path.Combine(directory, path));
                        if (Directory.Exists(fullPath))
                        {
                            // Add all files in the directory and its subdirectories to ignoredPaths
                            var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                ignoredPaths.Add(Path.GetFullPath(file));
                            }
                            Console.WriteLine($"Ignored directory: {fullPath}");
                        }
                        else
                        {
                            // Add the single file to ignoredPaths
                            ignoredPaths.Add(fullPath);
                            Console.WriteLine($"Ignored file: {fullPath}");
                        }
                    }
                }
            }

            return ignoredPaths;
        }


        private List<List<string>> DetectCycles(Dictionary<string, List<string>> graph)
        {
            var cycles = new List<List<string>>();
            var visited = new HashSet<string>();
            var stack = new Stack<string>();

            foreach (var node in graph.Keys)
            {
                if (DetectCyclesHelper(node, graph, visited, stack, new HashSet<string>(), cycles))
                {
                    var cycle = stack.Reverse().ToList();
                    cycles.Add(cycle);
                }
            }

            return cycles;
        }

        private bool DetectCyclesHelper(string node, Dictionary<string, List<string>> graph, HashSet<string> visited, Stack<string> stack, HashSet<string> recursionStack, List<List<string>> cycles)
        {
            if (recursionStack.Contains(node))
            {
                stack.Push(node); // Add node to stack to complete the cycle
                return true;
            }

            if (visited.Contains(node))
            {
                return false;
            }

            visited.Add(node);
            recursionStack.Add(node);
            stack.Push(node);

            if (graph.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (DetectCyclesHelper(neighbor, graph, visited, stack, recursionStack, cycles))
                    {
                        return true;
                    }
                }
            }

            stack.Pop();
            recursionStack.Remove(node);

            return false;
        }
    }
}
