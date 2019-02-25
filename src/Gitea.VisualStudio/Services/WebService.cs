using Gitea.VisualStudio.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Gitea.VisualStudio.Services
{
    [Export(typeof(IWebService))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class WebService : IWebService
    {
        [Import]
        private IStorage _storage;

        List<Project> lstProject = new List<Project>();
        DateTime dts = DateTime.MinValue;

        public async Task<Ownership[]> GetUserOrginizationsAsync()
        {
            var user = _storage.GetUser();
            if (user == null)
            {
                throw new UnauthorizedAccessException(Strings.WebService_CreateProject_NotLoginYet);
            }
            API.v1.Client client = CreateClient(user.Host, user.Username, user.Password);
            var usex = await client.Users.GetCurrent();
            List<Ownership> orgs = new List<Ownership>();
            foreach (var item in await usex.Repositories.GetUserOrginizationsAsync())
            {
                orgs.Add(new Ownership(item.Username, item.FullName, OwnershipTypes.Organization));
            }
            return orgs.ToArray();
        }

        public async Task<IReadOnlyList<Project>> GetProjects()
        {
            List<Project> lstpjt = new List<Project>();
            var user = _storage.GetUser();
            if (user == null)
            {
                throw new UnauthorizedAccessException(Strings.WebService_CreateProject_NotLoginYet);
            }
            API.v1.Client client = CreateClient(user.Host, user.Username, user.Password);
            var usex = await client.Users.GetCurrent();
            
            var projectt = await usex.Repositories.GetAll();
            if (projectt != null)
            {
                foreach (var item in projectt)
                {
                    lstpjt.Add(item);
                }
            }
            return lstpjt;
        }

        public async Task<User> LoginAsync(bool enable2fa, string host, string email, string password)
        {
            API.v1.Client client = CreateClient(host, email, password);
            User user = null;
            try
            {
                user = await client.Users.GetCurrent();
                user.Password = password;
            }
            catch (Exception ex)
            {
                throw new Exception($"错误: {ex.Message}");
            }
            return user;
        }

        private static API.v1.Client CreateClient(string host, string email, string password)
        {
            var uri = new Uri(host);
            API.v1.Client client = new API.v1.Client(email, password, uri.Host, uri.Port, uri.Scheme.EndsWith("s", true, null));
            return client;
        }

         
        
        public IReadOnlyList<NamespacesPath> GetNamespacesPathList()
        {

            return new List<NamespacesPath>();
        }
        public async Task<CreateProjectResult> CreateProjectAsync(string name, string description, bool isPrivate, string namespaceid, Ownership owner)
        {
            var user = _storage.GetUser();
            if (user == null)
            {
                throw new UnauthorizedAccessException(Strings.WebService_CreateProject_NotLoginYet);
            }
            var result = new CreateProjectResult();
            try
            {
                if (string.IsNullOrEmpty(namespaceid))
                {
                    namespaceid = user.Username;
                }
                API.v1.Client client = CreateClient(user.Host, user.Username, user.Password);
                var u = await client.Users.GetCurrent();
                var pjt = await u.Repositories.Create()
                    .Name(name)
                    .Description(description)
                    .MakeAutoInit(false)
                    .Owner(owner.UserName, owner.OwnerType == OwnershipTypes.Organization)
                     .Start();
                result.Project = (Project)pjt;
            }
            catch (Exception ex)
            {

                result.Message = ex.Message;
            }
            return result;
        }


        public async Task<Project> GetActiveProjectAsync(ProjectListType projectListType)
        {
            using (GitAnalysis ga = new GitAnalysis(GiteaPackage.GetSolutionDirectory()))
            {
                var url = ga.GetRepoOriginRemoteUrl();
                var pjts = await this.GetProjects();
                var pjt = from project in pjts where string.Equals(project.Url, url, StringComparison.OrdinalIgnoreCase) select project;
                return pjt.FirstOrDefault();
            }
        }
    }
}
