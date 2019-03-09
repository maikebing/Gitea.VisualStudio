using EnvDTE;
using EnvDTE100;
using EnvDTE80;
using Gitea.VisualStudio.Helpers;
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Task = System.Threading.Tasks.Task;
namespace Gitea.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideBindingPath]
    [InstalledProductRegistration("#110", "#112", PackageVersion.Version, IconResourceID = 400)]
    [Guid(PackageGuids.guidGitea4VSPkgString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // this is the Git service GUID, so we load whenever it loads
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    public class GiteaPackage : AsyncPackage,IVsInstalledProduct
    {
        [Import]
        private IShellService _shell;

        [Import]
        private IViewFactory _viewFactory;

        [Import]
        private IWebService _webService;

        [Import]
        private IStorage _storage;

        public GiteaPackage()
        {
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            }
        }

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
            pbstrName =  GetResourceString("@101");
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

        public  string   GetResourceString(string resourceName)
        {
            string resourceValue;
           
            var resourceManager =  (IVsResourceManager)GetService(typeof(SVsResourceManager));
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

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
           
            OutputWindowHelper.ExceptionWriteLine("Diagnostics mode caught and marked as handled the following DispatcherUnhandledException raised in Visual Studio", e.Exception);
            e.Handled = true;
        }

       
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.RunAsync(VsTaskRunContext.UIThreadNormalPriority, async delegate
            {
                // Added the following line to prevent the error "Due to high risk of deadlock you cannot call GetService from a background thread in an AsyncPackage derived class"
                var assemblyCatalog = new AssemblyCatalog(typeof(GiteaPackage).Assembly);
                CompositionContainer container = new CompositionContainer(assemblyCatalog);
                container.ComposeParts(this);
                var mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (mcs != null)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    try
                    {

                        new []{
                            PackageIds.OpenMaster,
                            PackageIds.OpenBranch,
                            PackageIds.OpenRevision,
                            PackageIds.OpenRevisionFull,
                            PackageIds.OpenBlame,
                            PackageIds.OpenCommits,
                            PackageIds.OpenFromUrl
                        }.ToList().ForEach(item =>
                        {
                            var menuCommandID = new CommandID(PackageGuids.guidGitea4VSCmdSet, (int)item);
                            var menuItem = new OleMenuCommand(ExecuteCommand, menuCommandID);
                            menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
                            mcs.AddCommand(menuItem);
                        });

                    }
                    catch (Exception ex)
                    {
                        OutputWindowHelper.DiagnosticWriteLine(ex.Message);
                    }
                }
                else
                {
                    OutputWindowHelper.DiagnosticWriteLine("mcs 为空");
                }
            });
       
        }

        private DTE2 _ide;
        public DTE2 DTE
        {
            get
            {
                if (_ide == null)
                {
                    ThreadHelper.JoinableTaskFactory.Run(async delegate
                    {
                        _ide = (DTE2)await GetServiceAsync(typeof(DTE));
                    });
                }
                return _ide;
            }
        }

        private IComponentModel _componentModel;

        public IComponentModel ComponentModel =>
           _componentModel ?? (_componentModel = GetGlobalService(typeof(SComponentModel)) as IComponentModel);

        public string GetActiveFilePath()
        {
            string path = "";
            if (DTE != null)
            {
                // sometimes, DTE.ActiveDocument.Path is ToLower but GitLab can't open lower path.
                // fix proper-casing | http://stackoverflow.com/questions/325931/getting-actual-file-name-with-proper-casing-on-windows-with-net
                path = GetExactPathName(DTE.ActiveDocument.Path + DTE.ActiveDocument.Name);
            }
            return path;
        }
        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            try
            {
                switch ((uint)command.CommandID.ID)
                {
                    case PackageIds.OpenFromUrl:
                        try
                        {
                            var match = Regex.Match(Clipboard.GetText(TextDataFormat.Text), "[a-zA-z]+://[^\\s]*");
                            command.Enabled = match.Success;
                            if (command.Enabled)
                            {
                                Uri uri = new Uri(match.Value);
                                command.Text =string.Format( Strings.OpenFrom0,uri.Host);
                            }
                            else
                            {
                                command.Text =Strings.OpenFromURL;
                            }
                        }
                        catch (Exception ex)
                        {
                            OutputWindowHelper.WarningWriteLine($"QueryStatus:{command.CommandID.ID},{ex.Message}");
                        }
                        break;
                        case PackageIds.OpenBlame:
                    case PackageIds.OpenBranch:
                    case PackageIds.OpenCommits:
                    case PackageIds.OpenMaster:
                    case PackageIds.OpenRevision:
                    case PackageIds.OpenRevisionFull:
                        {
                            try
                            {
                                var git = GitAnalysis.GetBy(GetActiveFilePath());
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
                            catch (Exception ex)
                            {
                                OutputWindowHelper.WarningWriteLine($"QueryStatus:{command.CommandID.ID},{ex.Message}");
                            }
                        }
                        break;
                     default:
                        break;
                }
            }
            catch (Exception ex)
            {
                var exstr = ex.ToString();
                Debug.Write(exstr);
                command.Text = "error:" + ex.GetType().Name;
                command.Enabled = false;
                OutputWindowHelper.WarningWriteLine(ex.Message);
            }
        }

        private void ExecuteCommand(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            try
            {
                switch ((uint)command.CommandID.ID)
                {
                    case PackageIds.OpenFromUrl:
                        if (Clipboard.ContainsText(TextDataFormat.Text))
                        {
                            var match = Regex.Match(Clipboard.GetText(TextDataFormat.Text), "[a-zA-z]+://[^\\s]*");
                            if (match.Success)
                            {
                                try
                                {
                                    TryOpenFile( match.Value);
                                }
                                catch (Exception ex)
                                {
                                    OutputWindowHelper.ExceptionWriteLine(string.Format("Can't Open {0},Exception:{1}", match.Value, ex.Message), ex);
                                }
                            }
                        }
                        break;
                    case PackageIds.OpenBlame:
                    case PackageIds.OpenBranch:
                    case PackageIds.OpenCommits:
                    case PackageIds.OpenMaster:
                    case PackageIds.OpenRevision:
                    case PackageIds.OpenRevisionFull:
                        {
                            try
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
                            catch (Exception ex) 
                            {
                                OutputWindowHelper.ExceptionWriteLine(string.Format("ExecuteCommand {0}", command.CommandID.ID, ex.Message),ex);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }
        }
        public void TryOpenFile(string url)
        {
            Uri uri = new Uri(url);
            using (var git = new GitAnalysis(GetActiveFilePath()))
            {
                if (git.IsDiscoveredGitRepository)
                {
                    var blob = Regex.Match(url, "/blob/(?<treeish>[^/]*)/");
                    if (blob.Success)
                    {
                        string p1 = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped).ToString();
                        string p2 = p1.Substring(p1.IndexOf(blob.Value) + blob.Value.Length);
                        var path = System.IO.Path.Combine(System.IO.Path.GetFullPath(System.IO.Path.Combine(git.RepositoryPath, "../")), p2);
                        var textView = OpenDocument(path);
                    }
                }
            }
        }
        IVsTextView OpenDocument(string fullPath)
        {
            var logicalView = VSConstants.LOGVIEWID.TextView_guid;
            IVsUIHierarchy hierarchy;
            uint itemID;
            IVsWindowFrame windowFrame;
            IVsTextView view;
            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, fullPath, logicalView, out hierarchy, out itemID, out windowFrame, out view);
            return view;
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
            if (commandId == PackageIds.OpenMaster) return GiteaUrlType.Master;
            if (commandId == PackageIds.OpenBranch) return GiteaUrlType.CurrentBranch;
            if (commandId == PackageIds.OpenRevision) return GiteaUrlType.CurrentRevision;
            if (commandId == PackageIds.OpenRevisionFull) return GiteaUrlType.CurrentRevisionFull;
            if (commandId == PackageIds.OpenBlame) return GiteaUrlType.Blame;
            if (commandId == PackageIds.OpenCommits) return GiteaUrlType.Commits;
            else return GiteaUrlType.Master;
        }
    }
}
