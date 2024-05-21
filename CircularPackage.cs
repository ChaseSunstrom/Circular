using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;

namespace Circular
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CircularPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class CircularPackage : AsyncPackage
    {
        private SolutionEventsListener _solutionEventsListener;
        private DTE2 _dte;

        public const string PackageGuidString = "6e31c8a6-1a02-48a6-80d7-64fd99a7bc94";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _dte = await GetServiceAsync(typeof(DTE)) as DTE2;
            var checker = new DependencyChecker();
            _solutionEventsListener = new SolutionEventsListener(_dte, checker);
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
