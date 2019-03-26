﻿using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Controls;
using Gitea.VisualStudio.Shared.Helpers;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Gitea.TeamFoundation.Home
{
    public abstract class GiteaNavigationItem : TeamExplorerNavigationItemBase, ITeamExplorerNavigationItem2
    {
        private readonly IGitService _git;
        private readonly IShellService _shell;
        private readonly IStorage _storage;
        private readonly ITeamExplorerServices _tes;
        private readonly IWebService _web;

        private Project _project;
        private string _branch;
        private Octicon octicon;

        public GiteaNavigationItem(Octicon icon, IGitService git, IShellService shell, IStorage storage, ITeamExplorerServices tes, IWebService web)
        {
            _git = git;
            _shell = shell;
            _storage = storage;
            _tes = tes;
            _web = web;
            octicon = icon;
            var brush = new SolidColorBrush(Color.FromRgb(66, 66, 66));
            brush.Freeze();
            OnThemeChanged();
            VSColorTheme.ThemeChanged += _ =>
            {
                OnThemeChanged();
                Invalidate();
            };
            var gitExt = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService<Microsoft.VisualStudio.TeamFoundation.Git.Extensibility.IGitExt>();
            gitExt.PropertyChanged += GitExt_PropertyChanged;
        }

        private void GitExt_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveRepositories")
            {
                Task.Run(async () =>
                {
                    await ThreadingHelper.SwitchToMainThreadAsync();
                    Invalidate();
                });
            }
        }

        public override async void Invalidate()
        {
            IsVisible = false;
            IsVisible = await _tes.IsGiteaRepoAsync() && _tes.Project != null;
        }

        private void OnThemeChanged()
        {
            var theme = Colors.DetectTheme();
            var dark = theme == "Dark";
            m_defaultArgbColorBrush = new SolidColorBrush(dark ? Colors.DarkThemeNavigationItem : Colors.LightBlueNavigationItem);
            m_icon = SharedResources.GetDrawingForIcon(octicon, dark ? Colors.DarkThemeNavigationItem : Colors.LightThemeNavigationItem, theme);
        }

        protected void OpenInBrowser(string endpoint)
        {
            var url = $"{_tes.Project.WebUrl}/{endpoint}";
            _shell.OpenUrl(url);
        }

        protected void OpenHostUrlInBrowser(string endpoint)
        {
            var url = $"{_storage.Host}/{endpoint}";
            _shell.OpenUrl(url);
        }
    }
}