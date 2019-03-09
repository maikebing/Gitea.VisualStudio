﻿using Gitea.TeamFoundation.Views;
using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Helpers;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;

namespace Gitea.TeamFoundation.Home
{
    [TeamExplorerSection(Settings.HomeSectionId, TeamExplorerPageIds.Home, Settings.HomeSectionPriority)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class GiteaHomeSection : TeamExplorerSectionBase
    {
        private readonly ITeamExplorerServices _tes;

        [ImportingConstructor]
        public GiteaHomeSection(ITeamExplorerServices tes)
        {
            _tes = tes;
        }

        public override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            IsVisible = false;
            base.Initialize(sender, e);
        }
        public override async void Refresh()
        {
            IsVisible =  await _tes.IsGiteaRepoAsync() ;
            var view = (this.View as TextBlock);
            if (view != null)
            {
                view.Text =( _tes.Project != null && !string.IsNullOrEmpty(_tes.Project.Description) )? _tes.Project.Description : Strings.Description;
            }
            base.Refresh();
        }

        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            var temp = new TeamExplorerSectionViewModelBase();
            temp.Title = Strings.Name;

            return temp;
        }

        protected override object CreateView(SectionInitializeEventArgs e)
        {
            return new TextBlock
            {
                Text = (_tes.Project != null && !string.IsNullOrEmpty(_tes.Project.Description)) ? _tes.Project.Description : Strings.Description, 
                TextWrapping = System.Windows.TextWrapping.Wrap
            };
        }
   
    }
}