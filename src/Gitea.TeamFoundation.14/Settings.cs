using Microsoft.TeamFoundation.Controls;

namespace Gitea.TeamFoundation
{
    static class Settings
    {
        public const string InvitationSectionId = "C2443FCC-6D62-4D31-B08A-C4DE70109C7F";
        
        public const int InvitationSectionPriority = 100;

        public const string ConnectSectionId = "5C4EAF08-7C8D-449F-B83E-21B7C3BDC545";
        public const int ConnectSectionPriority = 10;

        public const string HomeSectionId = "BECCEDE5-596C-41D0-B4AD-7DF99C1E7E40";
        public const int HomeSectionPriority = 10;

        public const string PublishSectionId = "E21E14A2-0A32-4709-8FEF-60FDBD1C169E";
        public const int PublishSectionPriority = 10;

        public const string IssuesNavigationItemId = "F771CAE9-6DCA-48E1-A0E9-80CE039F6083";
        public const int Issues = TeamExplorerNavigationItemPriority.GitCommits - 1;

        public const string MergeRequestsNavigationItemId = "2DD834C8-FDEB-4D17-919D-37F2380C599B";
        public const int MergeRequests = TeamExplorerNavigationItemPriority.GitCommits - 2;

       
        public const string WikiNavigationItemId = "B9D15991-1B18-4FDF-B94E-90CFEEF2C702";
        public const int Wiki = TeamExplorerNavigationItemPriority.Settings - 4;

        public const string ReleasesNavigationItemId = "E4103C71-9C3A-4C4A-A829-72F29B337A42";
        public const int Releases = TeamExplorerNavigationItemPriority.Settings - 5;

        public const string ActivityiNavigationItemId = "FFCABF9D-A679-4331-8521-9DF3BFE626F3";
        public const int Activity = TeamExplorerNavigationItemPriority.Settings - 6;
        
    }
}
