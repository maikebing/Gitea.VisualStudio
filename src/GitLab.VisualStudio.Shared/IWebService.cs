using Gitea.VisualStudio.Shared.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitea.VisualStudio.Shared
{
    public class User
    {

        public static implicit operator User(Gitea.API.v1.Users.User session)
        {
            if (session != null)
            {
                return new User()
                {
                    AvatarUrl = session.AvatarUrl,
                    Email = session.Email,
                    Id = (int)session.ID,
                    Name = session.Username,
                    TwoFactorEnabled = false,
                    Username = session.Username
                };
            }
            else
            {
                return null;
            }
        }
        public int Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public string PrivateToken { get; set; }
        public string Host { get; set; }
        public bool TwoFactorEnabled { get; set; }

    }

    public class Project
    {
        public static implicit operator Project(Gitea.API.v1.Repositories.Repository p)
        {
            if (p != null)
            {

                return new Project()
                {
                    BuildsEnabled = false,
                    Fork = p.IsFork,
                    HttpUrl = p.CloneUrl,
                    IssuesEnabled = true,
                    Name = p.Name,
                    Owner = p.Owner,
                    Public = !p.IsPrivate,
                    SnippetsEnabled = false,
                    SshUrl = p.SSHUrl,
                    WikiEnabled = true,
                    Id = (int)p.ID,
                    WebUrl = p.HtmlUrl
                };

            }
            else
            {
                return null;
            }
        }

        public int Id { get; set; }


        public string Name { get; set; }

        public string Path { get; set; }

        public bool Public { get; set; }
        public string SshUrl { get; set; }
        public string HttpUrl { get; set; }
        public string WebUrl { get; set; }
        public User Owner { get; set; }

        public bool Fork { get; set; }

        public bool IssuesEnabled { get; set; }

        public bool MergeRequestsEnabled { get; set; }

        public bool WikiEnabled { get; set; }
        public bool BuildsEnabled { get; set; }
        public bool SnippetsEnabled { get; set; }

        public string Url
        {
            get { return HttpUrl; }
        }


        public string LocalPath { get; set; }


        public Octicon Icon
        {
            get
            {
                return Public ? Octicon.@lock
                    : Fork
                    ? Octicon.repo_forked
                    : Octicon.repo;
            }
        }
    }

    public class CreateProjectResult
    {
        public string Message { get; set; }
        public Project Project { get; set; }
    }
    public class NamespacesPath
    {


        public int id { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public string kind { get; set; }
        public string full_path { get; set; }

    }

    public enum ProjectListType
    {
        Accessible,
        Owned,
        Membership,
        Starred,
        Forked
    }
    public interface IWebService
    {
        Task<User> LoginAsync(bool enable2fa, string host, string email, string password);
        Task<IReadOnlyList<Project>> GetProjects();
        Task<CreateProjectResult> CreateProjectAsync(string name, string description, bool isPrivate, string namespaceid);
        IReadOnlyList<NamespacesPath> GetNamespacesPathList();
    }
}
