using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitea.VisualStudio
{
    static class PackageGuids
    {
        public const string guidGiteaPkgString = "62991164-1F3E-4AAA-BF2F-537C83AF987C";
        public const string guidOpenOnGiteaPkgString = "45C0689E-851C-4558-BF56-0FA1BC29363F";
        public const string guidOpenOnGiteaCmdSetString = "29117BB3-5A68-49C0-82E7-B7FE8B9F11C6";

        public static readonly Guid guidOpenOnGiteaCmdSet = new Guid(guidOpenOnGiteaCmdSetString);
    };

    static class PackageCommanddIDs
    {
        public const uint OpenMaster = 0x100;
        public const uint OpenBranch = 0x200;
        public const uint OpenRevision = 0x300;
        public const uint OpenRevisionFull = 0x400;
        public const uint OpenBlame = 0x500;
        public const uint OpenCommits = 0x600;
        public const uint CreateSnippet = 0x700;
    };

 
}
