using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System;

namespace Circular
{
    public class SolutionEventsListener : IDisposable
    {
        private readonly DTE2 _dte;
        private readonly DependencyChecker _checker;

        public SolutionEventsListener(DTE2 dte, DependencyChecker checker)
        {
            _dte = dte;
            _checker = checker;
            _dte.Events.BuildEvents.OnBuildBegin += OnBuildBegin;
        }

        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var solutionDir = Path.GetDirectoryName(_dte.Solution.FullName);
                _checker.CheckDependencies(solutionDir);
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    ServiceProvider.GlobalProvider,
                    $"Error during circular dependency check: {ex.Message}",
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
