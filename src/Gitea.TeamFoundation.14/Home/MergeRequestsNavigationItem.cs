﻿using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Controls;
using Microsoft.TeamFoundation.Controls;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;

namespace Gitea.TeamFoundation.Home
{
    [TeamExplorerNavigationItem(Settings.MergeRequestsNavigationItemId, Settings.MergeRequests)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MergeRequestsNavigationItem : GiteaNavigationItem
    {
        private readonly ITeamExplorerServices _tes;

        [ImportingConstructor]
        public MergeRequestsNavigationItem(IGitService git, IShellService shell, IStorage storage, ITeamExplorerServices tes, IWebService ws)
           : base(Octicon.git_pull_request, git, shell, storage, tes, ws)
        {
            _tes = tes;
            Text = Strings.Items_MergeRequests;
        }

        public override void Invalidate()
        {
            base.Invalidate();

            IsVisible = IsVisible && _tes.Project != null && _tes.Project.MergeRequestsEnabled;
        }

        public override void Execute()
        {
            OpenInBrowser("pulls");
        }
    }
}