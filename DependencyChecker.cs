
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

            foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cpp") || f.EndsWith(".c") || f.EndsWith(".cc") || f.EndsWith(".h") || f.EndsWith(".hpp") || f.EndsWith(".hh")))
            {
                var includes = FindIncludes(file);
                graph[file] = includes;
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

        private List<List<string>> DetectCycles(Dictionary<string, List<string>> graph)
        {
            var cycles = new List<List<string>>();
            var visited = new HashSet<string>();
            var stack = new Stack<string>();

            foreach (var node in graph.Keys)
            {
                if (DetectCyclesHelper(node, graph, visited, stack, new HashSet<string>(), cycles))
                {
                    // Add the cycle path to the list of cycles
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
