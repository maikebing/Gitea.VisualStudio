using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Helpers;
using Gitea.VisualStudio.Shared.Helpers.Commands;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;

namespace Gitea.VisualStudio.UI.ViewModels
{
    public class CreateViewModel : Validatable
    {
        private readonly IDialog _dialog;
        private readonly IGitService _git;
        private readonly IMessenger _messenger;
        private readonly IShellService _shell;
        private readonly IStorage _storage;
        private readonly IWebService _web;

        public CreateViewModel(IDialog dialog, IGitService git, IMessenger messenger, IShellService shell, IStorage storage, IWebService web)
        {
            _dialog = dialog;
            _git = git;
            _messenger = messenger;
            _shell = shell;
            _storage = storage;
            _web = web;

            GitIgnores = new Dictionary<string, string>();
            Licenses = new Dictionary<string, string>();
            Namespaces=new    Dictionary<string, string>();
            _path = storage.GetBaseRepositoryDirectory();

            LoadResources();
            
            _newCommand = new DelegateCommand(OnSave, CanSave);
            _browseCommand = new DelegateCommand(OnBrowse);
        }

        private void LoadResources()
        {
            Owners.Clear();
            var user = _storage.GetUser();
            Owners.Add(new Ownership(user.Username, user.FullName, OwnershipTypes.User));
            SelectedOwner = Owners[0];
            Task.Run(async () =>
            {
                var owners = await _web.GetUserOrginizationsAsync();
                await ThreadingHelper.SwitchToMainThreadAsync();
                
                foreach (var owner in owners)
                {
                    Owners.Add(owner);
                }


            });

            GitIgnores.Add(string.Empty, Strings.Common_ChooseAGitIgnore);
            SelectedGitIgnore = string.Empty;
            foreach (var line in _git.GetGitIgnores())
            {
                GitIgnores.Add(line, $"{line} - .gitignore");
            }

            Licenses.Add(string.Empty, Strings.Common_ChooseALicense);
            SelectedLicense = string.Empty;
            foreach (var line in _git.GetLicenses())
            {
                Licenses.Add(line, line);
            }
            string defaultnamespace = _storage.GetUser().Username;
        
            foreach (var path in _web.GetNamespacesPathList())
            {
                Namespaces.Add(path.id.ToString(), $"{path.name} - {path.full_path}");
                if (path.full_path== defaultnamespace)
                {
                    SelectedNamespaces = path.id.ToString();
                }
            }
        }

        private string _name;

        [Required(ErrorMessageResourceType =typeof(Strings), ErrorMessageResourceName = "CreateView_NameIsRequired")]
        [MaxLength(64, ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = "Common_NameMaxTo")]
        public string Name
        {
            get { return _name; }
            set
            {
                SetProperty(ref _name, value, () =>
                {
                    _newCommand.InvalidateCanExecute();
                });
            }
        }

        private string _description;

        [MaxLength(500, ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = "Common_DescriptionMaxTo")]
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
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

        private string _path;

        [Required(ErrorMessageResourceType = typeof(Strings), ErrorMessageResourceName = "CreateView_PathIsRequired")]
        public string Path
        {
            get { return _path; }
            set
            {
                SetProperty(ref _path, value, () =>
                {
                    _newCommand.InvalidateCanExecute();
                });
            }
        }

        public IDictionary<string, string> GitIgnores { get; }
        public IDictionary<string, string> Namespaces { get; }

        private string _selectedGitIgnore;
        public string SelectedGitIgnore
        {
            get { return _selectedGitIgnore; }
            set { SetProperty(ref _selectedGitIgnore, value); }
        }

        public IDictionary<string, string> Licenses { get; }

        private string _selectedLicense;
        public string SelectedLicense
        {
            get { return _selectedLicense; }
            set { SetProperty(ref _selectedLicense, value); }
        }

        private DelegateCommand _browseCommand;
        public ICommand BrowseCommand
        {
            get { return _browseCommand; }
        }

        private DelegateCommand _newCommand;
        public ICommand NewCommand
        {
            get { return _newCommand; }
        }
        private string _selectedNamespaces;
        public string SelectedNamespaces
        {
            get { return _selectedNamespaces; }
            set { SetProperty(ref _selectedNamespaces, value); }
        }

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

        private void OnBrowse()
        {
            var browsed = _shell.BrowseFolder();
            if (browsed != null)
            {
                Path = browsed;
            }
        }

        private void OnSave()
        {
            CreateProjectResult result = null;
            string error = null;
            string clonePath = null;
            IsBusy = true;

            Task.Run(async () =>
            {
                try
                {
                    var pjts = await _web.GetProjects();
                    if (pjts.Any(p => p.Name == Name))
                    {
                        error = string.Format(Strings.CreateViewModel_OnSave_TheProject0AlreadyExists, Name);
                    }
                    else
                    {
                        result = await _web.CreateProjectAsync(Name, Description, IsPrivate,SelectedNamespaces, SelectedOwner);
                        if (result.Project != null)
                        {
                            clonePath = System.IO.Path.Combine(Path, result.Project.Name);

                            InitialCommit(result.Project.Url);
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }).ContinueWith(task =>
            {
                IsBusy = false;
                if (error != null)
                {
                    _dialog.Error(error);
                }
                else if (result.Message != null)
                {
                    _dialog.Error(result.Message);
                }
                else
                {
                    var repository = new Repository
                    {
                        Name = result.Project.Name,
                        Path = clonePath,
                        Icon = result.Project.Icon
                    };

                    _messenger.Send("OnClone", result.Project.Url, repository);

                    _dialog.Close();
                    _storage.Configuration.LocalRepoPath = Path;
                    _storage.SaveConfiguration();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void InitialCommit(string url)
        {
            var user = _storage.GetUser();
            
            _git.PushInitialCommit(user.Name, user.Email,user.Username, user.Password, url, SelectedGitIgnore, SelectedLicense);
          
        }

        private bool CanSave()
        {
            Validate();

            return !HasErrors;
        }
    }
}
