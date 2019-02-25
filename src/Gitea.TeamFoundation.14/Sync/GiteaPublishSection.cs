using Gitea.TeamFoundation.ViewModels;
using Gitea.TeamFoundation.Views;
using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Helpers;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;

namespace Gitea.TeamFoundation.Sync
{
    [TeamExplorerSection(Settings.PublishSectionId, TeamExplorerPageIds.GitCommits, Settings.PublishSectionPriority)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class GiteaPublishSection : TeamExplorerSectionBase
    {

        private readonly IMessenger _messenger;
        private readonly IGitService _git;
        private readonly IShellService _shell;
        private readonly IStorage _storage;
        private readonly ITeamExplorerServices _tes;
        private readonly IViewFactory _viewFactory;
        private readonly IWebService _web;

        [ImportingConstructor]
        public GiteaPublishSection(IMessenger messenger, IGitService git, IShellService shell, IStorage storage, ITeamExplorerServices tes, IViewFactory viewFactory,  IWebService web)
        {
            _messenger = messenger;
            _git = git;
            _shell = shell;
            _storage = storage;
            _tes = tes;
            _viewFactory = viewFactory;
            _web = web;
           
        }

        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            var temp = new TeamExplorerSectionViewModelBase
            {
                Title = string.Format(Strings.Publish_Title, Strings.Name)
            };

            return temp;
        }

        public override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            base.Initialize(sender, e);
            IsVisible = _tes.CanPublishGitea();
            var gitExt = ServiceProvider.GetService<Microsoft.VisualStudio.TeamFoundation.Git.Extensibility.IGitExt>();
            gitExt.PropertyChanged += GitExt_PropertyChanged;
        }

        private void GitExt_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
            return new PublishSectionView();
        }

        protected override void InitializeView(SectionInitializeEventArgs e)
        {
            var view = this.SectionContent as FrameworkElement;
            if (view != null)
            {
                var temp = new PublishSectionViewModel(_messenger, _git, _shell, _storage, _tes, _viewFactory, _web);
                temp.Published += OnPublished;
                view.DataContext = temp;
            }
        }

        private void OnPublished()
        {
            IsVisible = false;
        }

        public void ShowPublish()
        {
            IsVisible = true;
        }

        public override void Refresh()
        {
            var view = this.SectionContent as FrameworkElement;
            if (view != null)
            {
                var temp = view.DataContext as PublishSectionViewModel;
                temp.Refresh();
            }
            IsVisible = _tes.CanPublishGitea();
            base.Refresh();
        }


        public override void Dispose()
        {
            base.Dispose();

            var disposable = ViewModel as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}