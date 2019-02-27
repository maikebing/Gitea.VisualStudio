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

        public string DisplayName
        {
            get
            {
                return string.IsNullOrWhiteSpace(FullName) ? UserName : FullName;
            }
        }

        public OwnershipTypes OwnerType { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
