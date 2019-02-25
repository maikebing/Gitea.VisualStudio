using Gitea.TeamFoundation.ViewModels;
using Gitea.TeamFoundation.Views;
using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Helpers;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.Git.Controls.Extensibility;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;

namespace Gitea.TeamFoundation.Connect
{
    [TeamExplorerSection(Settings.ConnectSectionId, TeamExplorerPageIds.Connect, Settings.ConnectSectionPriority)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class GiteaConnectSection : TeamExplorerSectionBase
    {
        private readonly IMessenger _messenger;
        private readonly IShellService _shell;
        private readonly IStorage _storage;
        private readonly ITeamExplorerServices _teamexplorer;
        private readonly IViewFactory _viewFactory;
        private readonly IWebService _web;
        
        [ImportingConstructor]
        public GiteaConnectSection(IMessenger messenger, IShellService shell, IStorage storage, ITeamExplorerServices teamexplorer, IViewFactory viewFactory,  IWebService web)
        {
            _messenger = messenger;
            _shell = shell;
            _storage = storage;
            _teamexplorer = teamexplorer;
            _viewFactory = viewFactory;
            _web = web;
            
            messenger.Register("OnLoggedIn", OnLoggedIn);
            messenger.Register("OnSignedOut", InLoggedOut);
            messenger.Register<string, Repository>("OnClone", OnClone);
            messenger.Register<string>("OnOpenSolution", OnOpenSolution);
            
        }
        
        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            var temp = new TeamExplorerSectionViewModelBase
            {
                Title = Strings.Name
            };

            return temp;
        }

        public override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            base.Initialize(sender, e);
            //IsVisible = _storage.IsLogined;
            var gitExt = ServiceProvider.GetService<Microsoft.VisualStudio.TeamFoundation.Git.Extensibility.IGitExt>();
            gitExt.PropertyChanged += GitExt_PropertyChanged;
        }

        private void GitExt_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveRepositories")
            {
                Task.Run(async () =>
                {
                    await ThreadingHelper.SwitchToMainThreadAsync();
                    Refresh();
                });
            }
        }

        protected override object CreateView(SectionInitializeEventArgs e)
        {
            return new ConnectSectionView();
        }
        protected override void InitializeView(SectionInitializeEventArgs e)
        {
            var view = this.SectionContent as FrameworkElement;
            if (view != null)
            {
                view.DataContext = new ConnectSectionViewModel(_messenger, _shell, _storage, _teamexplorer, _viewFactory, _web);
            }
        }

        public void OnLoggedIn()
        {
            // Added Connect and Sign Up buttons in case user closes the invitation.
            //IsVisible = true;
        }

        public void InLoggedOut()
        {
            // Added Connect and Sign Up buttons in case user closes the invitation.
            //IsVisible = false;
        }

        public void OnClone(string url, Repository repository)
        {
            var gitExt = ServiceProvider.GetService<IGitRepositoriesExt>();
            gitExt.Clone(url, repository.Path, CloneOptions.RecurseSubmodule);
        }

        public void OnOpenSolution(string path)
        {
            var x = ServiceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (x != null)
            {
                x.OpenSolutionViaDlg(path, 1);
            }
        }

       
        public override void Refresh()
        {
            
            ((View as ConnectSectionView).DataContext as ConnectSectionViewModel).Refresh();
            
            base.Refresh();
        }

        

        public override void Dispose()
        {
            _messenger.UnRegister(this);

            var disposable = ViewModel as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
