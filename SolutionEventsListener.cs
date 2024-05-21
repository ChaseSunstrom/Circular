using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Circular
{
    public class SolutionEventsListener : IDisposable
    {
        private readonly DTE2 _dte;
        private readonly DependencyChecker _checker;
        private readonly IVsOutputWindowPane _outputWindowPane;
        private readonly IVsTaskList _taskList;
        private uint _providerCookie;

        public SolutionEventsListener(DTE2 dte, DependencyChecker checker, IVsOutputWindowPane outputWindowPane, IVsTaskList taskList)
        {
            _dte = dte;
            _checker = checker;
            _outputWindowPane = outputWindowPane;
            _taskList = taskList;
            _dte.Events.BuildEvents.OnBuildBegin += OnBuildBegin;
            _taskList.RegisterTaskProvider(CircularPackage.PackageGuidString, "Circular Dependency Checker", null, out _providerCookie);
        }

        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var solutionDir = Path.GetDirectoryName(_dte.Solution.FullName);
                _outputWindowPane.OutputString("Starting circular dependency check...\n");

                // Clear existing tasks
                _taskList.RefreshTasks(_providerCookie);

                var circularDeps = _checker.CheckDependencies(solutionDir);
                if (circularDeps.Any())
                {
                    foreach (var cycle in circularDeps)
                    {
                        var errorText = $"Circular dependency detected: {string.Join(" -> ", cycle)}";
                        _outputWindowPane.OutputString(errorText + "\n");

                        // Add error task to error list
                        foreach (var file in cycle)
                        {
                            CreateErrorTask(file, errorText);
                        }
                    }
                }
                else
                {
                    _outputWindowPane.OutputString("Circular dependency check completed successfully.\n");
                }
            }
            catch (Exception ex)
            {
                var message = $"Error during circular dependency check: {ex.Message}";
                _outputWindowPane.OutputString(message + "\n");
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider.GlobalProvider,
                    message,
                    "Circular Dependency Checker",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private void CreateErrorTask(string filePath, string message)
        {
            var errorTask = new ErrorTask
            {
                Document = filePath,
                Category = TaskCategory.BuildCompile,
                ErrorCategory = TaskErrorCategory.Error,
                Text = message,
                Line = 0,
                Column = 0,
                HierarchyItem = null // You can get the IVsHierarchy item if needed
            };
            errorTask.Navigate += (sender, e) =>
            {
                NavigateTo(filePath, 0, 0);
            };
            _taskList.AddTask(errorTask);
        }

        private void NavigateTo(string filePath, int line, int column)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var window = _dte.ItemOperations.OpenFile(filePath);
            if (window != null)
            {
                var textSelection = (TextSelection)_dte.ActiveDocument.Selection;
                textSelection.MoveToLineAndOffset(line + 1, column + 1);
            }
        }

        public void Dispose()
        {
            _dte.Events.BuildEvents.OnBuildBegin -= OnBuildBegin;
            if (_providerCookie != 0)
            {
                _taskList.UnregisterTaskProvider(_providerCookie);
                _providerCookie = 0;
            }
        }
    }
}