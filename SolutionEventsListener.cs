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

        public SolutionEventsListener(DTE2 dte, DependencyChecker checker, IVsOutputWindowPane outputWindowPane)
        {
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
