
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
        private readonly ErrorListProvider _errorListProvider;

        public SolutionEventsListener(DTE2 dte, DependencyChecker checker, IVsOutputWindowPane outputWindowPane, ErrorListProvider errorListProvider)
        {
            _errorListProvider = errorListProvider;
            _dte = dte;
            _checker = checker;
            _outputWindowPane = outputWindowPane;
            _dte.Events.BuildEvents.OnBuildBegin += OnBuildBegin;
        }

        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var solutionDir = Path.GetDirectoryName(_dte.Solution.FullName);
                _outputWindowPane.OutputString("Starting circular dependency check...\n");
                _checker.CheckDependencies(solutionDir);
                _outputWindowPane.OutputString("Circular dependency check completed successfully.\n");
            }
            catch (Exception ex)
            {
                // Add this block to report the error to the Error List window
                ErrorTask errorTask = new ErrorTask
                {
                    Text = ex.Message,
                    ErrorCategory = TaskErrorCategory.Error,
                    Document = "", // Set this to the path of the document where the error occurred
                    Line = 0, // Set this to the line number where the error occurred
                    Column = 0, // Set this to the column number where the error occurred
                    HierarchyItem = null // Set this to the IVsHierarchy of the project where the error occurred
                };
                _errorListProvider.Tasks.Add(errorTask);
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

        public void Dispose()
        {
            _dte.Events.BuildEvents.OnBuildBegin -= OnBuildBegin;
        }
    }
}
