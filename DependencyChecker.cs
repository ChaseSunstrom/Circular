
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
            var projectDirectory = directory;

            foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Select(f => Path.GetFullPath(f))
                .Where(f => f.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".c", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".cc", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".h", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".hpp", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".hh", StringComparison.OrdinalIgnoreCase))
                .Where(f => !ignoredPaths.Contains(f)))
            {
                var includes = FindIncludes(file, projectDirectory);
                graph[file] = includes
                    .Where(i => !ignoredPaths.Contains(i))
                    .ToList();
            }
            return graph;
        }

        private List<string> FindIncludes(string filePath, string projectDirectory)
        {
            var includes = new List<string>();
            var content = File.ReadAllText(filePath);
            var includeMatches = Regex.Matches(content, @"#include\s*([""<])(.*?)[>""]");

            foreach (Match match in includeMatches)
            {
                var delimiter = match.Groups[1].Value;
                var includeFile = match.Groups[2].Value.Trim();

                string fullPath = null;
                if (delimiter == "\"")
                {
                    // Local include: resolve relative to the including file
                    fullPath = NormalizePath(filePath, includeFile);
                }
                else if (delimiter == "<")
                {
                    // System include: attempt to resolve within the project directory
                    fullPath = Path.GetFullPath(Path.Combine(projectDirectory, includeFile));
                }

                if (fullPath != null && File.Exists(fullPath))
                {
                    includes.Add(fullPath);
                }
            }

            return includes;
        }

        private string NormalizePath(string filePath, string includePath)
        {
            if (Path.IsPathRooted(includePath))
            {
                return Path.GetFullPath(includePath);
            }
            else
            {
                return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filePath), includePath));
            }
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
                        else if (File.Exists(fullPath))
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

            foreach (var node in graph.Keys)
            {
                var recursionStack = new HashSet<string>();
                var path = new List<string>();
                DFS(node, graph, visited, recursionStack, path, cycles);
            }

            return cycles;
        }

        private void DFS(string node, Dictionary<string, List<string>> graph, HashSet<string> visited, HashSet<string> recursionStack, List<string> path, List<List<string>> cycles)
        {
            if (recursionStack.Contains(node))
            {
                int index = path.IndexOf(node);
                if (index != -1)
                {
                    var cycle = path.Skip(index).ToList();
                    cycle.Add(node); // Close the cycle
                    cycles.Add(cycle);
                }
                return;
            }

            if (visited.Contains(node))
            {
                return;
            }

            visited.Add(node);
            recursionStack.Add(node);
            path.Add(node);

            if (graph.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    DFS(neighbor, graph, visited, recursionStack, path, cycles);
                }
            }

            recursionStack.Remove(node);
            path.RemoveAt(path.Count - 1);
        }
    }

}
