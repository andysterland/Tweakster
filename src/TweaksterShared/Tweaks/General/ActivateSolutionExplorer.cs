﻿using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Tweakster
{
    internal sealed class FocusSolutionExplorer
    {
        private static AsyncPackage _package;
        private static IVsUIShell _uiShell;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _package = package;
            _uiShell = await package.GetServiceAsync<SVsUIShell, IVsUIShell>();
            IVsSolution solService = await _package.GetServiceAsync<SVsSolution, IVsSolution>();

            if (solService.IsOpen())
            {
                Execute();
            }

            SolutionEvents.OnAfterBackgroundSolutionLoadComplete += Execute;
        }

        private static void Execute(object sender = null, EventArgs e = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (Options.Instance.FocusSolutionExplorerOnProjectLoad)
            {
                Guid guid = typeof(VSConstants.VSStd97CmdID).GUID;
                var id = (uint)VSConstants.VSStd97CmdID.ProjectExplorer;

                _uiShell.PostExecCommand(guid, id, 0, null);
            }
        }
    }
}
