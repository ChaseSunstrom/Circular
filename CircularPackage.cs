using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Circular
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CircularPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class CircularPackage : AsyncPackage
    {
        private SolutionEventsListener _solutionEventsListener;
        private DTE2 _dte;
        private IVsOutputWindowPane _outputWindowPane;
        private IVsTaskList _taskList;

        public const string PackageGuidString = "6e31c8a6-1a02-48a6-80d7-64fd99a7bc94";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _dte = await GetServiceAsync(typeof(DTE)) as DTE2;
            var checker = new DependencyChecker();

            // Initialize the output window pane
            var outputWindow = await GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var paneGuid = new Guid("0F3B825F-3456-4538-BFA9-9AA9C5A438A7"); // Use a unique GUID
            outputWindow.CreatePane(ref paneGuid, "Circular Dependency Checker", 1, 1);
            outputWindow.GetPane(ref paneGuid, out _outputWindowPane);

            // Initialize the task list
            _taskList = await GetServiceAsync(typeof(SVsTaskList)) as IVsTaskList;

            _solutionEventsListener = new SolutionEventsListener(_dte, checker, _outputWindowPane, _taskList);
        }

        protected override void Dispose(bool disposing)
        {
            if (_solutionEventsListener != null)
            {
                _solutionEventsListener.Dispose();
                _solutionEventsListener = null;
            }
            base.Dispose(disposing);
        }
    }
}