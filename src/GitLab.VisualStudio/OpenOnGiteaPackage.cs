using EnvDTE; 
using EnvDTE80;
using Gitea.VisualStudio;
using Gitea.VisualStudio.Services;
using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.UI.ViewModels;
using Gitea.VisualStudio.UI.Views;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace Gitea.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#8110", "#8112", PackageVersion.Version, IconResourceID = 8400)]
    [ProvideMenuResource("Menus2.ctmenu", 1)]
    [Guid(PackageGuids.guidOpenOnGiteaPkgString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed class OpenOnGiteaPackage : Package
    {
      
 

        [Import]
        private IShellService _shell;
     
        [Import]
        private IViewFactory _viewFactory;

        private static DTE2 _dte;
        internal static DTE2 DTE
        {
            get
            {
                if (_dte == null)
                {
                    _dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
                }

                return _dte;
            }
        }
        
        protected override void Initialize()
        {
            base.Initialize();
            var assemblyCatalog = new AssemblyCatalog(typeof(OpenOnGiteaPackage).Assembly);
            CompositionContainer container = new CompositionContainer(assemblyCatalog);
            container.ComposeParts(this);
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
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
                    using (var git = new GitAnalysis(GetActiveFilePath()))
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
                    var selection = DTE.ActiveDocument.Selection as TextSelection;
                    if (selection != null)
                    {
                        //var dialog = _viewFactory.GetView<Dialog>(ViewTypes.CreateSnippet);
                        //var cs = (CreateSnippet)dialog;
                        //var csm = cs.DataContext as CreateSnippetViewModel;
                        //csm.Code = selection.Text;
                        //csm.FileName = new System.IO.FileInfo(DTE.ActiveDocument.FullName).Name; 
                        //_shell.ShowDialog(Strings.OpenOnGiteaPackage_CreateSnippet, dialog);
                    }
                    else
                    {
                        Debug.Write("未选择任何内容");
                    }
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

        string GetActiveFilePath()
        {
            // sometimes, DTE.ActiveDocument.Path is ToLower but Gitea can't open lower path.
            // fix proper-casing | http://stackoverflow.com/questions/325931/getting-actual-file-name-with-proper-casing-on-windows-with-net
            var path = GetExactPathName(DTE.ActiveDocument.Path + DTE.ActiveDocument.Name);
            return path;
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
