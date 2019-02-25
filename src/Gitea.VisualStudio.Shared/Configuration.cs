using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitea.VisualStudio.Shared
{
    public class Configuration
    {
        public string Host { get; set; }

        public string LocalRepoPath { get; set; }

        public User User { get; set; }
    }
}
