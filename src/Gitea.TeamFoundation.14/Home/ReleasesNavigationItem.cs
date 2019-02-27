﻿using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Controls;
using Microsoft.TeamFoundation.Controls;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace Gitea.TeamFoundation.Home
{
    [TeamExplorerNavigationItem(Settings.ReleasesNavigationItemId, Settings.Releases)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ReleasesNavigationItem : GiteaNavigationItem
    {
        private readonly ITeamExplorerServices _tes;

        [ImportingConstructor]
        public ReleasesNavigationItem(IGitService git, IShellService shell, IStorage storage, ITeamExplorerServices tes, IWebService ws)
           : base(Octicon.book, git, shell, storage, tes, ws)
        {
            _tes = tes;

            Text = Gitea.VisualStudio.Shared.Strings.Releases;
        }

        public override void Invalidate()
        {
            base.Invalidate();
            IsVisible = IsVisible && _tes.Project != null  ;
        }

        public override void Execute()
        {
            OpenInBrowser("releases");
        }
    }
}
      