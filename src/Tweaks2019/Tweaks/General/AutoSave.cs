﻿using System;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Tweakster
{
    public class AutoSave
    {
        private static IVsUIShell _uiShell;
        private static DTE2 _dte;
        private static WindowEvents _windowEvents;
        private static ProjectItemsEvents _projectsEvents;
        private static BuildEvents _buildEvents;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _uiShell = await package.GetServiceAsync<SVsUIShell, IVsUIShell>();
            _dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            Assumes.Present(_dte);

            _buildEvents = _dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += OnBuildBegin;

            _windowEvents = _dte.Events.WindowEvents;
            _windowEvents.WindowActivated += OnWindowActivated;

            _projectsEvents = (_dte.Events as Events2).ProjectItemsEvents;
            _projectsEvents.ItemAdded += (p) => OnProjectItemChange(p);
            _projectsEvents.ItemRemoved += (p) => OnProjectItemChange(p);
            _projectsEvents.ItemRenamed += (p, n) => OnProjectItemChange(p);
        }

        private static void OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Options.Instance.SaveAllOnBuild)
            {
                Guid guid = typeof(VSConstants.VSStd97CmdID).GUID;
                var id = (uint)VSConstants.VSStd97CmdID.SaveSolution;
                _uiShell.PostExecCommand(guid, id, 0, null);
            }
        }

        private static void OnProjectItemChange(ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (ShouldExecute(item))
                {
                    if (item.ContainingProject.IsDirty)
                    {
                        item.ContainingProject.Save();
                    }
                }
            }
            catch
            {
                // Do nothing. Some project types don't support being saved
            }
        }

        private static void OnWindowActivated(Window gotFocus, Window lostFocus)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ShouldExecute(lostFocus?.Document?.ProjectItem))
            {
                if (!lostFocus.Document.Saved)
                {
                    lostFocus.Document.Save();
                }

                // Test IsDirty to filter out temp and misc project types
                if (lostFocus.Project?.IsDirty == true)
                {
                    lostFocus.Project.Save();
                }
            }
        }

        private static bool ShouldExecute(ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return Options.Instance.AutoSave &&
                   _dte.Mode == vsIDEMode.vsIDEModeDesign &&
                   item?.ContainingProject != null &&
                   item?.ContainingProject?.Kind != ProjectKinds.vsProjectKindSolutionFolder;
        }
    }
}
