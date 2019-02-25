using EnvDTE;
using EnvDTE80;
using Gitea.VisualStudio.Services;
using Gitea.VisualStudio.Shared;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Task = System.Threading.Tasks.Task;
namespace Gitea.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", PackageVersion.Version, IconResourceID = 400)]
    [Guid(PackageGuids.guidGiteaPkgString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // this is the Git service GUID, so we load whenever it loads
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    public class GiteaPackage : AsyncPackage,IVsInstalledProduct
    {

        #region IVsInstalledProduct Members

        public int IdBmpSplash(out uint pIdBmp)
        {
            pIdBmp = 400;
            return VSConstants.S_OK;
        }

        public int IdIcoLogoForAboutbox(out uint pIdIco)
        {
            pIdIco = 400;
            return VSConstants.S_OK;
        }

        public int OfficialName(out string pbstrName)
        {
            pbstrName = GetResourceString("@101");
            return VSConstants.S_OK;
        }

        public int ProductDetails(out string pbstrProductDetails)
        {
            pbstrProductDetails = Vsix.Description;
            return VSConstants.S_OK;
        }

        public int ProductID(out string pbstrPID)
        {
            pbstrPID = Vsix.Id;
            return VSConstants.S_OK;
        }

        public string GetResourceString(string resourceName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string resourceValue;

            var resourceManager = (IVsResourceManager)GetService(typeof(SVsResourceManager));
            if (resourceManager == null)
            {
                throw new InvalidOperationException(
                    "Could not get SVsResourceManager service. Make sure that the package is sited before calling this method");
            }

            Guid packageGuid = GetType().GUID;
            int hr = resourceManager.LoadResourceString(
                ref packageGuid, -1, resourceName, out resourceValue);
            ErrorHandler.ThrowOnFailure(hr);

            return resourceValue;
        }

        #endregion IVsInstalledProduct Members

        private DTE2 _dte;

        internal DTE2 DTE
        {
            get
            {
                if (_dte == null)
                {
                    ThreadHelper.JoinableTaskFactory.Run(async delegate
                    {
                        _dte = (DTE2)await GetServiceAsync(typeof(DTE));
                    });
                }
                return _dte;
            }
        }
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.RunAsync(VsTaskRunContext.UIThreadNormalPriority, async delegate
            {
                var assemblyCatalog = new AssemblyCatalog(typeof(GiteaPackage).Assembly);
                CompositionContainer container = new CompositionContainer(assemblyCatalog);
                container.ComposeParts(this);
                var mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (mcs != null)
                {
                    foreach (var item in new[]
                    {
                    PackageCommanddIDs.OpenMaster,
                    PackageCommanddIDs.OpenBranch,
                    PackageCommanddIDs.OpenRevision,
                    PackageCommanddIDs.OpenRevisionFull,
                     PackageCommanddIDs.OpenBlame,
                     PackageCommanddIDs.OpenCommits

                })
                    {
                        var menuCommandID = new CommandID(PackageGuids.guidOpenOnGiteaCmdSet, (int)item);
                        var menuItem = new OleMenuCommand(ExecuteCommand, menuCommandID);
                        menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
                        mcs.AddCommand(menuItem);
                    }
                }
            });
        }


        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            try
            {
                if (command.CommandID.ID == PackageCommanddIDs.CreateSnippet)
                {
                    command.Text = Strings.OpenOnGiteaPackage_CreateSnippet;
                    var selectionLineRange = GetSelectionLineRange();
                    command.Enabled = selectionLineRange.Item1 < selectionLineRange.Item2;
                }
                else
                {
                    // TODO:is should avoid create GitAnalysis every call?
                    using (var git =   GitAnalysis.GetBy(GetActiveFilePath()))
                    {
                        if (!git.IsDiscoveredGitRepository)
                        {
                            command.Enabled = false;
                            return;
                        }

                        var type = ToGiteaUrlType(command.CommandID.ID);
                        var targetPath = git.GetGiteaTargetPath(type);
                        if (type == GiteaUrlType.CurrentBranch && targetPath == "master")
                        {
                            command.Visible = false;
                        }
                        else
                        {
                            command.Text = git.GetGiteaTargetDescription(type);
                            command.Enabled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var exstr = ex.ToString();
                Debug.Write(exstr);
                command.Text = "error:" + ex.GetType().Name;
                command.Enabled = false;
            }
        }

        private void ExecuteCommand(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            try
            {

                if (command.CommandID.ID == PackageCommanddIDs.CreateSnippet)
                {
                    
                }
                else
                {
                    using (var git = new GitAnalysis(GetActiveFilePath()))
                    {
                        if (!git.IsDiscoveredGitRepository)
                        {
                            return;
                        }
                        var selectionLineRange = GetSelectionLineRange();
                        var type = ToGiteaUrlType(command.CommandID.ID);
                        var GiteaUrl = git.BuildGiteaUrl(type, selectionLineRange);
                        System.Diagnostics.Process.Start(GiteaUrl); // open browser
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }
        }

     

      
     
        public   string GetActiveFilePath()
        {
            string path = "";
            if (DTE != null)
            {
                // sometimes, DTE.ActiveDocument.Path is ToLower but Gitea can't open lower path.
                // fix proper-casing | http://stackoverflow.com/questions/325931/getting-actual-file-name-with-proper-casing-on-windows-with-net
                path = GetExactPathName(DTE.ActiveDocument.Path + DTE.ActiveDocument.Name);
            }
            return path;
        }
        public static string GetSolutionDirectory()
        {
            var det2 = (DTE2)GetGlobalService(typeof(DTE));
            var path = string.Empty;
            if (det2 != null && det2.Solution != null && det2.Solution.IsOpen)
            {
                path = new System.IO.FileInfo(det2.Solution.FileName).DirectoryName;
            }
            return path;
        }

        public static bool UrlEquals(string url1, string url2)
        {
            var uri1 = new Uri(url1.ToLower());
            var uri2 = new Uri(url2.ToLower());
            return uri1.PathAndQuery == uri2.PathAndQuery && uri1.Host == uri2.Host;
        }
        static string GetExactPathName(string pathName)
        {
            if (!(File.Exists(pathName) || Directory.Exists(pathName)))
                return pathName;

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetExactPathName(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }
            else
            {
                return di.Name.ToUpper();
            }
        }

        Tuple<int, int> GetSelectionLineRange()
        {
            var selection = DTE.ActiveDocument.Selection as TextSelection;
            if (selection != null)
            {
                if (!selection.IsEmpty)
                {
                    return Tuple.Create(selection.TopPoint.Line, selection.BottomPoint.Line);
                }
                else
                {
                    return Tuple.Create(selection.CurrentLine, selection.CurrentLine);
                }
            }
            else
            {
                return null;
            }
        }
        static GiteaUrlType ToGiteaUrlType(int commandId)
        {
            if (commandId == PackageCommanddIDs.OpenMaster) return GiteaUrlType.Master;
            if (commandId == PackageCommanddIDs.OpenBranch) return GiteaUrlType.CurrentBranch;
            if (commandId == PackageCommanddIDs.OpenRevision) return GiteaUrlType.CurrentRevision;
            if (commandId == PackageCommanddIDs.OpenRevisionFull) return GiteaUrlType.CurrentRevisionFull;
            if (commandId == PackageCommanddIDs.OpenBlame) return GiteaUrlType.Blame;
            if (commandId == PackageCommanddIDs.OpenCommits) return GiteaUrlType.Commits;
            else return GiteaUrlType.Master;
        }
    }
}
