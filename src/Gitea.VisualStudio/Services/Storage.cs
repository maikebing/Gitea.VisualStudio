using Gitea.VisualStudio.Shared;
using Microsoft.TeamFoundation.Git.Controls.Extensibility;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace Gitea.VisualStudio.Services
{
    [Export(typeof(IStorage))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Storage : IStorage
    {
        string _giteaPath = null;
        Configuration _configuration = null;

        public Storage()
        {
            LoadConfiguration();
        }

        public bool IsLogined
        {
            get
            {
                return !string.IsNullOrEmpty(Configuration.Host) && GetUser() != null;
            }
        }

        public string Host
        {
            get
            {
                return Configuration.Host;
                //string url = string.Empty;
                //using (var git = new GitAnalysis(GiteaPackage.GetSolutionDirectory()))
                //{
                //    if (git != null && git.IsDiscoveredGitRepository)
                //    {
                //        string hurl = git.GetRepoUrlRoot();
                //        if (!string.IsNullOrEmpty(hurl))
                //        {
                //            Uri uri = new Uri(hurl);
                //            url = $"{uri.Scheme}://{uri.Host}{(uri.Port == 80 || uri.Port == 443 ? "" : $":{uri.Port}")}";
                //        }
                //    }
                //}
                //if (string.IsNullOrEmpty(url))
                //{
                //    url = Configuration.Host;
                //}
                //return url;
            }
        }
        public string Path
        {
            get
            {
                if (string.IsNullOrEmpty(_giteaPath))
                {
                    _giteaPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".Gitea");
                }
                return _giteaPath;
            }
        }

        public Configuration Configuration { get => _configuration; }


        public User GetCredential(string host)
        {
            using (var credential = new Credential())
            {
                credential.Target = $"git:{host}";
                return credential.Load()
                    ? new User()
                    {
                        Host = host,
                        Username = credential.Username,
                        Name = credential.Username,
                        Password = credential.Password
                    }
                    : null;
            }
        }

        private string GetToken(string _host)
        {
            var key = $"token:{_host}";

            using (var credential = new Credential())
            {
                credential.Target = key;
                return credential.Load()
                    ? credential.Password
                    : null;
            }
        }

        public User GetUser()
        {
            string host = Host;
            using (var credential = new Credential())
            {
                credential.Target = $"git:{host}";
                if (credential.Load())
                {
                    if (host == Configuration?.User.Host)
                    {
                        User user = Configuration.User.Clone();
                        user.Password = credential.Password;
                        return user;
                    }
                    else
                    {
                        return new User()
                        {
                            Host = host,
                            Username = credential.Username,
                            Name = credential.Username,
                            Password = credential.Password
                        };
                    }
                }
            }
            return null;
        }

        public void SaveUser(string host, User user, string password)
        {
            SavePassword(host, user.Username, password);
            user.Host = host;
            Configuration.Host = host;
            Configuration.User = user;
            SaveConfiguration();
        }

        public void LoadConfiguration()
        {

            if (File.Exists(Path))
            {
                var serializer = new JsonSerializer();
                JObject giteaJson;
                using (var reader = new JsonTextReader(new StreamReader(Path)))
                {
                    _configuration = serializer.Deserialize<Configuration>(reader);
                }
            }
            else
            {
                _configuration = new Configuration();
            }
        }

        public void SaveConfiguration()
        {
            var serializer = new JsonSerializer();
            using (var writer = new JsonTextWriter(new StreamWriter(Path)))
            {
                writer.Formatting = Formatting.Indented;
                serializer.Serialize(writer, Configuration);
            }
        }


        private void SavePassword(string _host, string username, string password)
        {
            var key = $"git:{_host}";
            using (var credential = new Credential(username, password, key))
            {
                credential.Save();
            }
        }

        private void SaveToken(string _host, string username, string token)
        {
            var key = $"token:{_host}";
            using (var credential = new Credential(username, token, key))
            {
                credential.Save();
            }
        }

        private User LoadUser()
        {
            User _user = null;
            try
            {


            }
            catch (Exception ex)
            {


            }
            return _user;
        }

        public void Erase()
        {
            EraseCredential($"git:{Host}");
            EraseCredential($"token:{Host}");
            _configuration = new Configuration();
            SaveConfiguration();
        }

        private static void EraseCredential(string key)
        {
            using (var credential = new Credential())
            {
                credential.Target = key;
                credential.Delete();
            }
        }

        public string GetBaseRepositoryDirectory()
        {
            if (!string.IsNullOrEmpty(Configuration.LocalRepoPath) && Directory.Exists(Configuration.LocalRepoPath))
            {
                return Configuration.LocalRepoPath;
            }
            string _path = this.Path;
            if (!System.IO.Directory.Exists(_path))
            {
                var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                _path = System.IO.Path.Combine(user, "Source", "Repos");
            }

            return _path;
        }
    }
}
