using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.UI.ViewModels;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System;

namespace Gitea.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Dialog, IPasswordMediator
    {
        public LoginView(IMessenger messenger, IShellService shell, IStorage storage, IWebService web)
        {
            InitializeComponent();

            var vm = new LoginViewModel(this, this, messenger, shell, storage, web);

            DataContext = vm;
        }

        public string Password
        {
            get { return PasswordTextBox.Text; }
            set { PasswordTextBox.Text = value; }
        }

        

        private void PasswordTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SignInButton.Command.Execute(null);
            }
        }
    }
}
