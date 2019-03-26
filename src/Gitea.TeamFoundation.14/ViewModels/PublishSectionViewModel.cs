using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Helpers;
using Gitea.VisualStudio.Shared.Helpers.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace Gitea.TeamFoundation.ViewModels
{
    public class PublishSectionViewModel : Bindable, IDisposable
    {
        private readonly IMessenger _messenger;
        private readonly IGitService _git;
        private readonly IShellService _shell;
        private readonly IStorage _storage;
        private readonly ITeamExplorerServices _tes;
        private readonly IViewFactory _viewFactory;
        private readonly IWebService _web;

        public event Action Published;

        private void OnPublished()
        {
            Published?.Invoke();
        }

        public PublishSectionViewModel(IMessenger messenger, IGitService git, IShellService shell, IStorage storage, ITeamExplorerServices tes, IViewFactory viewFactory, IWebService web)
        {
            messenger.Register("OnLoggedIn", OnLoggedIn);
            messenger.Register("OnSignedOut", OnSignedOut);
            _messenger = messenger;
            _git = git;
            _shell = shell;
            _storage = storage;
            _tes = tes;
            _viewFactory = viewFactory;
            _web = web;
            Name = Strings.Name;
            if (string.IsNullOrEmpty(storage.Host))
            {
                Provider = Strings.Provider;
            }
            else
            {
                Provider = Strings.Provider + ": " + storage.Host + "";
            }

            Description = Strings.Description;
            _loginCommand = new DelegateCommand(OnLogin);
            _signUpCommand = new DelegateCommand(OnSignUp);
            _getStartedCommand = new DelegateCommand(OnGetStarted);
            _publishCommand = new DelegateCommand(OnPublish, CanPublish);
            ShowGetStarted = storage.IsLogined;
            //IsStarted = true;
            LoadResources();
        }

        private void LoadResources()
        {
            Licenses.Clear();
            Licenses.Add(string.Empty, Strings.Common_ChooseALicense);
            SelectedLicense = string.Empty;
            foreach (var line in _git.GetLicenses())
            {
                Licenses.Add(line, line);
            }
            Owners.Clear();
            var user = _storage.GetUser();
            if (user != null)
            {
                Owners.Add(new Ownership(user.Username, user.FullName, OwnershipTypes.User));
            }
            Task.Run(async () =>
            {
                var owners = await _web.GetUserOrginizationsAsync();
                await ThreadingHelper.SwitchToMainThreadAsync();

                foreach (var owner in owners)
                {
                    Owners.Add(owner);
                }
            });
        }

        public string Name { get; set; }
        public string Provider { get; set; }
        public string Description { get; set; }

        public ObservableCollection<Ownership> Owners { get; } = new ObservableCollection<Ownership>();

        private Ownership _selectedOwner = null;

        public Ownership SelectedOwner
        {
            get
            {
                return _selectedOwner;
            }
            set
            {
                SetProperty(ref _selectedOwner, value);
            }
        }

        internal void Refresh()
        {
            ShowGetStarted = _storage.IsLogined;
        }

        public IDictionary<string, string> Licenses { get; } = new Dictionary<string, string>();

        private string _selectedLicense;

        public string SelectedLicense
        {
            get { return _selectedLicense; }
            set { SetProperty(ref _selectedLicense, value); }
        }

        private bool _isStarted;

        public bool IsStarted
        {
            get { return _isStarted; }
            set { SetProperty(ref _isStarted, value); }
        }

        public bool ShowLogin => !_storage.IsLogined;

        public bool ShowSignUp => !_storage.IsLogined;

        private bool _showGetStarted;

        public bool ShowGetStarted
        {
            get { return _showGetStarted; }
            set { SetProperty(ref _showGetStarted, value); }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        private bool _isPrivate;

        public bool IsPrivate
        {
            get { return _isPrivate; }
            set { SetProperty(ref _isPrivate, value); }
        }

        private DelegateCommand _loginCommand;

        public ICommand LoginCommand
        {
            get
            {
                return _loginCommand;
            }
        }

        private DelegateCommand _signUpCommand;

        public ICommand SignUpCommand
        {
            get
            {
                return _signUpCommand;
            }
        }

        private DelegateCommand _getStartedCommand;

        public ICommand GetStartedCommand
        {
            get
            {
                return _getStartedCommand;
            }
        }

        private DelegateCommand _publishCommand;

        public ICommand PublishCommand
        {
            get { return _publishCommand; }
        }

        private string _repositoryName;

        public string RepositoryName
        {
            get { return _repositoryName; }
            set
            {
                SetProperty(ref _repositoryName, value, () =>
                {
                    _publishCommand.InvalidateCanExecute();
                });
            }
        }

        private string _repositoryDescription;

        public string RepositoryDescription
        {
            get { return _repositoryDescription; }
            set { SetProperty(ref _repositoryDescription, value); }
        }

        private void OnLogin()
        {
            var dialog = _viewFactory.GetView<Dialog>(ViewTypes.Login);
            _shell.ShowDialog(string.Format(Strings.Login_ConnectTo, Strings.Name), dialog);
        }

        public void OnLoggedIn()
        {
            OnPropertyChanged(nameof(ShowLogin));
            OnPropertyChanged(nameof(ShowSignUp));
            LoadResources();
            ShowGetStarted = true;
        }

        public void OnSignedOut()
        {
            OnPropertyChanged(nameof(ShowLogin));
            OnPropertyChanged(nameof(ShowSignUp));

            ShowGetStarted = false;
        }

        private void OnSignUp()
        {
            if (string.IsNullOrEmpty(_storage.Host))
            {
                _shell.OpenUrl("https://visualstudio.giteahub.com/");
            }
            else
            {
                _shell.OpenUrl($"{_storage.Host}/users/sign_in");
            }
        }

        private void OnGetStarted()
        {
            if (!_storage.IsLogined)
            {
                var dialog = _viewFactory.GetView<Dialog>(ViewTypes.Login);
                _shell.ShowDialog(string.Format(Strings.Login_ConnectTo, Strings.Name), dialog);
            }

            if (_storage.IsLogined)
            {
                ShowGetStarted = false;
                IsStarted = true;
                if (Owners.Count > 0)
                {
                    SelectedOwner = Owners[0];
                }
                RepositoryName = System.IO.Path.GetFileNameWithoutExtension(_tes.GetSolutionFullPath());
            }
        }

        private void OnPublish()
        {
            CreateProjectResult result = null;
            string error = null;

            IsBusy = true;

            Task.Run(async () =>
            {
                try
                {
                    var user = _storage.GetUser();
                    var activeRepository = _tes.GetActiveRepository();
                    result = await _web.CreateProjectAsync(RepositoryName, RepositoryDescription, IsPrivate, user.Username, SelectedOwner);
                    if (result.Project != null)
                    {
                        var path = activeRepository == null ? _tes.GetSolutionPath() : activeRepository.Path;

                        _git.PushWithLicense(user.Name, user.Email, user.Username, user.Password, result.Project.Url, path, SelectedLicense);
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    _tes.ShowError(ex.Message);
                }
            }).ContinueWith(task =>
            {
                IsBusy = false;
                if (error != null)
                {
                    _tes.ShowError(error);
                }
                else if (result.Message != null)
                {
                    _tes.ShowError(result.Message);
                }
                else
                {
                    IsStarted = false;
                    ShowGetStarted = true;
                    OnPublished();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private bool CanPublish()
        {
            return _repositoryName != null && _repositoryName.Trim().Length < 64;
        }

        public void Dispose()
        {
            _messenger.UnRegister(this);
            GC.SuppressFinalize(this);
        }
    }
}