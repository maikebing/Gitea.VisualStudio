using System;

namespace Gitea.VisualStudio.Shared
{
    public interface IStorage
    {
        bool IsLogined { get; }
        User GetUser();
        string Host { get;  }
        string Path { get; }
        Configuration Configuration { get; }
        void SaveUser(string host, User user, string password);
        void LoadConfiguration();
        void SaveConfiguration();
        void Erase();

        string GetBaseRepositoryDirectory();
    }
}
