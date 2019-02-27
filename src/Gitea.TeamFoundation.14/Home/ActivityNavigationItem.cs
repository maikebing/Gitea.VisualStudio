using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Controls;
using Microsoft.TeamFoundation.Controls;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace Gitea.TeamFoundation.Home
{
    [TeamExplorerNavigationItem(Settings.ActivityiNavigationItemId, Settings.Activity)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ActivityNavigationItem : GiteaNavigationItem
    {
        private readonly ITeamExplorerServices _tes;

        [ImportingConstructor]
        public ActivityNavigationItem(IGitService git, IShellService shell, IStorage storage, ITeamExplorerServices tes, IWebService ws)
           : base(Octicon.book, git, shell, storage, tes, ws)
        {
            _tes = tes;

            Text = Strings.Activity;
        }

        public override void Invalidate()
        {
            base.Invalidate();
            IsVisible = IsVisible && _tes.Project != null && _tes.Project.WikiEnabled;
        }

        public override void Execute()
        {
            OpenInBrowser("activity");
        }
    }
}
      