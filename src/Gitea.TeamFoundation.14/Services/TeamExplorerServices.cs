using Gitea.TeamFoundation.Sync;
using Gitea.VisualStudio.Shared;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Gitea.TeamFoundation
{
    [Export(typeof(ITeamExplorerServices))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class TeamExplorerServices : ITeamExplorerServices
    {
        private readonly IServiceProvider serviceProvider;

        [Import]
        private IGitService _git;

        [Import]
        private IWebService _web;

        /// <summary>
        /// This MEF export requires specific versions of TeamFoundation. ITeamExplorerNotificationManager is declared here so
        /// that instances of this type cannot be created if the TeamFoundation dlls are not available
        /// (otherwise we'll have multiple instances of ITeamExplorerServices exports, and that would be Bad(tm))
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private ITeamExplorerNotificationManager manager;

        [ImportingConstructor]
        public TeamExplorerServices([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void ShowPublishSection()
        {
            var te = serviceProvider.TryGetService<ITeamExplorer>();
            var foo = te.NavigateToPage(new Guid(TeamExplorerPageIds.GitCommits), null);
            var publish = foo?.GetSection(new Guid(Settings.PublishSectionId)) as GiteaPublishSection;
            publish?.ShowPublish();
        }

        public void ShowMessage(string message)
        {
            manager = serviceProvider.TryGetService<ITeamExplorer>() as ITeamExplorerNotificationManager;
            manager?.ShowNotification(message, NotificationType.Information, NotificationFlags.None, null, default(Guid));
        }

        public void ShowMessage(string message, ICommand command)
        {
            manager = serviceProvider.TryGetService<ITeamExplorer>() as ITeamExplorerNotificationManager;
            manager?.ShowNotification(message, NotificationType.Information, NotificationFlags.None, command, default(Guid));
        }

        public void ShowWarning(string message)
        {
            manager = serviceProvider.TryGetService<ITeamExplorer>() as ITeamExplorerNotificationManager;
            manager?.ShowNotification(message, NotificationType.Warning, NotificationFlags.None, null, default(Guid));
        }

        public void ShowError(string message)
        {
            manager = serviceProvider.TryGetService<ITeamExplorer>() as ITeamExplorerNotificationManager;
            manager?.ShowNotification(message, NotificationType.Error, NotificationFlags.None, null, default(Guid));
        }

        public void ClearNotifications()
        {
            manager = serviceProvider.TryGetService<ITeamExplorer>() as ITeamExplorerNotificationManager;
            manager?.ClearNotifications();
        }

        public RepositoryInfo GetActiveRepository()
        {
            if (serviceProvider == null)
            {
                return null;
            }

            var git = serviceProvider.GetService<IGitExt>();
            if (git == null)
            {
                throw new Exception("git is null");
            }

            if (git.ActiveRepositories == null)
            {
                throw new Exception("git.activeRepositories is null");
            }

            var repo = git.ActiveRepositories.FirstOrDefault();

            if (repo != null && repo.CurrentBranch != null && !string.IsNullOrEmpty(repo.CurrentBranch.Name))
            {
                return new RepositoryInfo
                {
                    Path = repo.RepositoryPath,
                    Branch = repo.CurrentBranch.Name
                };
            }
            return null;
        }

        public string GetSolutionPath()
        {
            if (serviceProvider == null)
            {
                return null;
            }
            var solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            if (!ErrorHandler.Succeeded(solution.GetSolutionInfo(out string solutionDir, out string solutionFile, out string userFile)))
            {
                return null;
            }

            if (solutionDir == null)
            {
                return null;
            }

            return solutionDir;
        }

        public string GetSolutionFullPath()
        {
            if (serviceProvider == null)
            {
                return null;
            }
            var solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            if (!ErrorHandler.Succeeded(solution.GetSolutionInfo(out string solutionDir, out string solutionFile, out string userFile)))
            {
                return null;
            }

            if (solutionDir == null || solutionFile == null)
            {
                return null;
            }

            return System.IO.Path.Combine(solutionDir, solutionFile);
        }

        public Project Project { get; private set; }

        public bool CanPublishGitea()
        {
            var repo = GetActiveRepository();
            if (repo == null)
            {
                return false;
            }
            var path = repo.Path;
            var url = _git.GetRemote(path);
            return url == null;
        }

        public async Task<bool> IsGiteaRepoAsync()
        {
            var repo = GetActiveRepository();
            if (repo == null)
            {
                return false;
            }
            return await IsGiteaRepoAsync(repo);
        }

        public async Task<bool> IsGiteaRepoAsync(RepositoryInfo repo)
        {
            var path = repo.Path;
            var url = _git.GetRemote(path);

            if (url == null)
            {
                return false;
            }

            if (Project == null || !string.Equals(Project.Url, url, StringComparison.OrdinalIgnoreCase))
            {
                await LoadGiteaProject(url);
            }
            bool isGitea = false;
            if (Project != null && !string.IsNullOrEmpty(Project.HttpUrl) && Uri.IsWellFormedUriString(Project.HttpUrl, UriKind.Absolute)
                && !string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                try
                {
                    UriBuilder remoteurl = new UriBuilder(url);
                    UriBuilder Giteaurl = new UriBuilder(Project.HttpUrl);
                    isGitea = remoteurl.Host.ToLower() == Giteaurl.Host.ToLower();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            return isGitea;
        }

        private async System.Threading.Tasks.Task LoadGiteaProject(string url)
        {
            try
            {
                var projects = await _web.GetProjects();
                foreach (var project in projects)
                {
                    if (string.Equals(project.Url, url, StringComparison.OrdinalIgnoreCase))
                    {
                        Project = project;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // Ignore
            }
        }
    }
}