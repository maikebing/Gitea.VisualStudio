using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Helpers;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Gitea.TeamFoundation.Connect
{
    [TeamExplorerServiceInvitation(Settings.InvitationSectionId, Settings.InvitationSectionPriority)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class GiteaInvitationSection : TeamExplorerServiceInvitationBase
    {
        private readonly IMessenger _messenger;
        private readonly IShellService _shell;
        private readonly IStorage _storage;
        private readonly IViewFactory _viewFactory;

        [ImportingConstructor]
        public GiteaInvitationSection(IMessenger messenger, IShellService shell, IStorage storage, IViewFactory viewFactory)
        {
            _messenger = messenger;
            _shell = shell;
            _storage = storage;
            _viewFactory = viewFactory;

            _messenger.Register("OnLoggedIn", OnLoggedIn);
            _messenger.Register("OnSignedOut", OnSignedOut);

            CanConnect = true;
            CanSignUp = true;
            ConnectLabel = Strings.Invitation_Connect;
            SignUpLabel = Strings.Invitation_SignUp;
            Name = Strings.Name;
            Provider = Strings.Provider + (storage.IsLogined ? "(" + storage.Host + ")" : Strings.GiteaInvitationSection_GiteaInvitationSection_NoLogin);
            Description = Strings.Description;
            var assembly = Assembly.GetExecutingAssembly().GetName().Name;
            var image = new BitmapImage(new Uri($"pack://application:,,,/{assembly};component/Resources/logo.png", UriKind.Absolute)); ;

            var drawing = new DrawingGroup();
            drawing.Children.Add(new GeometryDrawing
            {
                Brush = new ImageBrush(image),
                Geometry = new RectangleGeometry(new Rect(new Size(image.Width, image.Height)))
            });

            Icon = new DrawingBrush(drawing);

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
                    IsVisible = !_storage.IsLogined;
                });
            }
        }

        public override void Connect()
        {
            var dialog = _viewFactory.GetView<Dialog>(ViewTypes.Login);
            _shell.ShowDialog(string.Format(Strings.Login_ConnectTo, Strings.Name), dialog);
        }

        public override void SignUp()
        {
            _shell.OpenUrl($"{_storage.Host}/users/sign_in#register-pane");
        }

        public void OnLoggedIn()
        {
            IsVisible = false;
        }

        public void OnSignedOut()
        {
            IsVisible = true;
        }

        public override void Dispose()
        {
            base.Dispose();

            _messenger.UnRegister(this);
            GC.SuppressFinalize(this);
        }
    }
}