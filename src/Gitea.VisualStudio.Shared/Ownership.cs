using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitea.VisualStudio.Shared
{
    public enum OwnershipTypes
    {
        User,
        Organization
    }

    public class Ownership
    {
        public Ownership() { }

        public Ownership(string userName, string fullName, OwnershipTypes ownerType)
        {
            UserName = userName;
            FullName = fullName;
            OwnerType = ownerType;
        }

        public string UserName { get; set; }

        public string FullName { get; set; }

        public OwnershipTypes OwnerType { get; set; }
    }
}
