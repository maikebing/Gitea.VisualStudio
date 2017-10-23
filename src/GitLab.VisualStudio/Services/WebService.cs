using Gitea.VisualStudio.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        
     
            public async Task<IReadOnlyList<Project>> GetProjects( )
        {
            List<Project> lstpjt = new List<Project>();
            var user = _storage.GetUser();
            if (user == null)
            {
                throw new UnauthorizedAccessException(Strings.WebService_CreateProject_NotLoginYet);
            }
            API.v1.Client client = new API.v1.Client(user.Username, user.PrivateToken, user.Host);
            var usex = await client.Users.GetCurrent();
           var  projectt =await  usex.Repositories.GetAll();
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
            API.v1.Client client =  new API.v1.Client (email,password,host);
            User user = null;
            try
            {
                user  = await client.Users.GetCurrent();
                user.PrivateToken = password;
            }
            catch ( Exception ex)
            {
                throw new Exception($"错误: {ex.Message}");
            }
            return user;
        }
        public CreateProjectResult CreateProject(string name, string description, bool isPrivate)
        {
            return CreateProjectAsync(name, description, isPrivate, null);
        }
       public  IReadOnlyList<NamespacesPath> GetNamespacesPathList()
        {
            
            return  new List<NamespacesPath>();
        }
        public    CreateProjectResult CreateProjectAsync(string name, string description, bool isPrivate,string namespaceid)
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
                API.v1.Client client = new API.v1.Client(user.Username, user.PrivateToken, user.Host);
            //    var userx = await client.Users.GetCurrent();
               //API.v1.Repositories.Repository 
               // var pjt = await client.Users.GetCurrent().Create(
               //     new   ()
               //     {
               //         Description = description, Name = name, VisibilityLevel = isPrivate ? NGitea.Models.VisibilityLevel.Private : NGitea.Models.VisibilityLevel.Public
               //        , IssuesEnabled= true, ContainerRegistryEnabled=true, JobsEnabled=true, LfsEnabled=true, SnippetsEnabled =true, WikiEnabled=true, MergeRequestsEnabled=true 
               //             , NamespaceId = namespaceid
               //     });
               // result.Project = (Project)pjt;

            }
            catch (Exception ex)
            {

                result.Message = ex.Message;
            }
            return result;
        }
     
        
        public async Task<Project> GetActiveProjectAsync(ProjectListType projectListType )
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
