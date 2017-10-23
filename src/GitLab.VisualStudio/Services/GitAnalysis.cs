using Gitea.VisualStudio.Shared;
using LibGit2Sharp;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Gitea.VisualStudio.Services
{
    public enum GiteaUrlType
    {
        Master,
        CurrentBranch,
        CurrentRevision,
        CurrentRevisionFull,
        Blame,
        Commits,
    }

    public sealed class GitAnalysis : IDisposable
    {
        readonly LibGit2Sharp.Repository repository;
        readonly string targetFullPath;

        public bool IsDiscoveredGitRepository => repository != null;

        public GitAnalysis(string targetFullPath)
        {
            this.targetFullPath = targetFullPath;
            var repositoryPath = LibGit2Sharp.Repository.Discover(targetFullPath);
            if (repositoryPath != null)
            {
                this.repository = new LibGit2Sharp.Repository(repositoryPath);
                RepositoryPath = repositoryPath;
            }
            
            
        }
        public string RepositoryPath { get; private set; }
        public LibGit2Sharp.Repository Repository { get { return repository; } }

        public string GetGiteaTargetPath(GiteaUrlType urlType)
        {
            switch (urlType)
            {
                case GiteaUrlType.CurrentBranch:
                    return repository.Head.FriendlyName.Replace("origin/", "");
                case GiteaUrlType.CurrentRevision:
                    return repository.Commits.First().Id.ToString(8);
                case GiteaUrlType.CurrentRevisionFull:
                    return repository.Commits.First().Id.Sha;
                case GiteaUrlType.Master:
                default:
                    return "master";
            }
        }

        public string GetGiteaTargetDescription(GiteaUrlType urlType)
        {
            switch (urlType)
            {
                case GiteaUrlType.CurrentBranch:
                    return Strings.GitAnalysisn_Branch + repository.Head.FriendlyName.Replace("origin/", "");
                case GiteaUrlType.CurrentRevision:
                    return Strings.GitAnalysis_Revision + repository.Commits.First().Id.ToString(8);
                case GiteaUrlType.CurrentRevisionFull:
                    return Strings.GitAnalysis_Revision + repository.Commits.First().Id.ToString(8) + Strings.GitAnalysis_GetGiteaTargetDescription_FullID;
                case GiteaUrlType.Blame:
                    return Strings.GitAnalysis_Blame;
                case GiteaUrlType.Commits:
                    return Strings.GitAnalysis_Commits;
                case GiteaUrlType.Master:
                default:
                    return "master";
            }
        }

        public string BuildGiteaUrl(GiteaUrlType urlType, Tuple<int, int> selectionLineRange)
        {
            // https://Gitea.com/user/repo.git
            string urlRoot = GetRepoUrlRoot();

            // foo/bar.cs
            var rootDir = repository.Info.WorkingDirectory;
            var fileIndexPath = targetFullPath.Substring(rootDir.Length).Replace("\\", "/");

            var repositoryTarget = GetGiteaTargetPath(urlType);

            // line selection
            var fragment = (selectionLineRange != null)
                                ? (selectionLineRange.Item1 == selectionLineRange.Item2)
                                    ? string.Format("#L{0}", selectionLineRange.Item1)
                                    : string.Format("#L{0}-L{1}", selectionLineRange.Item1, selectionLineRange.Item2)
                                : "";

            var urlshowkind = "blob";
            if (urlType == GiteaUrlType.Blame)
            {
                urlshowkind = "blame";
            }
            if (urlType == GiteaUrlType.Commits)
            {
                urlshowkind = "commits";
            }
            var fileUrl = string.Format("{0}/{4}/{1}/{2}{3}", urlRoot.Trim('/'), WebUtility.UrlEncode(repositoryTarget.Trim('/')), fileIndexPath.Trim('/'), fragment, urlshowkind);

            return fileUrl;
        }

        public string GetRepoUrlRoot()
        {
            string urlRoot = string.Empty;
           var originUrl = repository.Config.Get<string>("remote.origin.url");
            if (originUrl!=null )
            {
                // https://Gitea.com/user/repo
                  urlRoot = (originUrl.Value.EndsWith(".git", StringComparison.InvariantCultureIgnoreCase))
                    ? originUrl.Value.Substring(0, originUrl.Value.Length - 4) // remove .git
                    : originUrl.Value;

                // git@Gitea.com:user/repo -> http://Gitea.com/user/repo
                urlRoot = Regex.Replace(urlRoot, "^git@(.+):(.+)/(.+)$", match => "http://" + string.Join("/", match.Groups.OfType<Group>().Skip(1).Select(group => group.Value)), RegexOptions.IgnoreCase);

                // https://user@Gitea.com/user/repo -> https://Gitea.com/user/repo
                urlRoot = Regex.Replace(urlRoot, "(?<=^https?://)([^@/]+)@", "");
            }
            return urlRoot;
        }
        public string GetRepoOriginRemoteUrl()
        {
            string urlRoot = string.Empty;
            var originUrl = repository.Config.Get<string>("remote.origin.url");
            if (originUrl != null)
            {
                urlRoot = originUrl.Value ;
            }
            return urlRoot;
        }
        void Dispose(bool disposing)
        {
            if (repository != null)
            {
                repository.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GitAnalysis()
        {
            Dispose(false);
        }
    }
}