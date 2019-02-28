using Gitea.TeamFoundation.Services;
using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Helpers;
using Gitea.VisualStudio.Shared.Helpers.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Gitea.TeamFoundation.ViewModels
{
    public class ConnectSectionViewModel : Bindable, IDisposable
    {
        public ObservableCollection<Repository> Repositories { get; }

        private readonly IMessenger _messenger;
        private readonly IShellService _shell;
        private readonly IStorage _storage;
        private readonly ITeamExplorerServices _teamexplorer;
        private readonly IViewFactory _viewFactory;
        private readonly IWebService _web;

        public ConnectSectionViewModel(IMessenger messenger, IShellService shell, IStorage storage, ITeamExplorerServices teamexplorer, IViewFactory viewFactory, IWebService web)
        {
            messenger.Register("OnLoggedIn", OnLoggedIn);
            messenger.Register("OnSignedOut", OnSignedOut);
            messenger.Register<string, Repository>("OnClone", OnRepositoryCloned);

            _messenger = messenger;
            _shell = shell;
            _storage = storage;
            _teamexplorer = teamexplorer;
            _viewFactory = viewFactory;
            _web = web;

            Repositories = new ObservableCollection<Repository>();

            Repositories.CollectionChanged += OnRepositoriesChanged;

            SignInCommand = new DelegateCommand(OnSignIn);
            SignUpCommand = new DelegateCommand(OnSignUp);
            _signOutCommand = new DelegateCommand(OnSignOut);
            _cloneCommand = new DelegateCommand(OnClone);
            _createCommand = new DelegateCommand(OnCreate);
            _openRepositoryCommand = new DelegateCommand<Repository>(OnOpenRepository);

            LoggedInButtonsVisible = _storage.IsLogined ? Visibility.Visible : Visibility.Collapsed;
            NotLoggedInButtonsVisible = LoggedInButtonsVisible == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            IsRepositoriesVisible = LoggedInButtonsVisible == Visibility.Visible;
            LoadRepositoriesAsync();
        }

        private void OnSignedOut()
        {
            NotLoggedInButtonsVisible = Visibility.Visible;
            LoggedInButtonsVisible = Visibility.Collapsed;
            IsRepositoriesVisible = false;
        }

        public void OnLoggedIn()
        {
            NotLoggedInButtonsVisible = Visibility.Collapsed;
            LoggedInButtonsVisible = Visibility.Visible;
            IsRepositoriesVisible = true;
            LoadRepositoriesAsync();
        }

        private void OnRepositoriesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsRepositoriesVisible));
        }

        private Repository _selectedRepository;
        public Repository SelectedRepository
        {
            get { return _selectedRepository; }
            set { SetProperty(ref _selectedRepository, value); }
        }

        public ICommand SignInCommand { get; }

        public ICommand SignUpCommand { get; }

        private DelegateCommand _signOutCommand;
        public ICommand SignOutCommand
        {
            get { return _signOutCommand; }
        }

        private DelegateCommand _cloneCommand;
        public ICommand CloneCommand
        {
            get { return _cloneCommand; }
        }

        private DelegateCommand _createCommand;
        public ICommand CreateCommand
        {
            get { return _createCommand; }
        }

        private DelegateCommand<Repository> _openRepositoryCommand;
        public ICommand OpenRepositoryCommand
        {
            get { return _openRepositoryCommand; }
        }

        private bool _isRepositoriesVisible;
        public bool IsRepositoriesVisible
        {
            get { return _isRepositoriesVisible; }
            set { SetProperty(ref _isRepositoriesVisible, value); }
        }

        private Visibility _notLoggedInButtonsVisible;

        public Visibility NotLoggedInButtonsVisible
        {
            get { return _notLoggedInButtonsVisible; }
            set { SetProperty(ref _notLoggedInButtonsVisible,  value); }
        }

        private Visibility _LoginButtonsVisible;

        public Visibility LoggedInButtonsVisible
        {
            get { return _LoginButtonsVisible; }
            set { SetProperty(ref _LoginButtonsVisible, value); }
        }

        public void OnSignIn()
        {
            var dialog = _viewFactory.GetView<Dialog>(ViewTypes.Login);
            _shell.ShowDialog(string.Format(Strings.Login_ConnectTo, Strings.Name), dialog);
        }

        public void OnSignUp()
        {
            if (string.IsNullOrEmpty(_storage.Host))
            {
                _shell.OpenUrl("https://giteahub.com/user/sign_up");
            }
            else
            {
                _shell.OpenUrl($"{_storage.Host}/user/sign_up");
            }
        }

        private void OnSignOut()
        {
            if (MessageBox.Show(Strings.Confirm_Quit, $"{Strings.Common_Quit} {Strings.Name}", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _storage.Erase();
                _messenger.Send("OnSignedOut");
            }
        }

        private void OnClone()
        {
            var dialog = _viewFactory.GetView<Dialog>(ViewTypes.Clone);
            _shell.ShowDialog(Strings.Common_Clone, dialog);
        }

        private void OnCreate()
        {
            var dialog = _viewFactory.GetView<Dialog>(ViewTypes.Create);
            _shell.ShowDialog(Strings.Common_CreateRepository, dialog);
        }

        private void OnOpenRepository(Repository repo)
        {
            if (repo == null)
            {
                return;
            }

            var solution = _teamexplorer.GetSolutionPath();
            if (solution == null || !string.Equals(repo.Path, solution.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                _messenger.Send("OnOpenSolution", repo.Path);
            }
        }

        private void LoadRepositoriesAsync()
        {
            IReadOnlyList<Repository> known = null;
            IReadOnlyList<Project> remotes = null;

            Exception ex = null;
            Task.Run(async () =>
            {
                try
                {
                    remotes =await _web.GetProjects();
                    known = Registry.GetKnownRepositories();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            }).ContinueWith(task =>
            {
                if (ex == null)
                {
                    Repositories.Clear();

                    var activeRepository = _teamexplorer.GetActiveRepository();

                    var valid = new List<Repository>();

                    if (known != null)
                    {
                        foreach (var k in known)
                        {
                            var r = remotes.FirstOrDefault(o => o.Name == k.Name);
                            if (r != null)
                            {
                                k.Icon = r.Icon;

                                valid.Add(k);
                            }
                        }
                    }

                    if (activeRepository != null)
                    {
                        var matched = valid.FirstOrDefault(o => string.Equals(o.Path, activeRepository.Path, StringComparison.OrdinalIgnoreCase));
                        if (matched != null)
                        {
                            matched.IsActived = true;
                        }
                    }

                    valid.Each(o => Repositories.Add(o));
                }
                else if (!(ex is UnauthorizedAccessException))
                {
                    _teamexplorer.ShowMessage(ex.Message);
                }

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void OnRepositoryCloned(string url, Repository repository)
        {
            Repositories.Add(repository);
            foreach (var r in Repositories)
            {
                r.IsActived = false;
            }

            repository.IsActived = true;
        }

        public void Refresh()
        {
            LoadRepositoriesAsync();
        }

        public void Dispose()
        {
            _messenger.UnRegister(this);
            GC.SuppressFinalize(this);
        }
    }
}
