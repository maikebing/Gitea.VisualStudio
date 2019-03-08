using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace Gitea.VisualStudio.Helpers
{
    internal static class OutputWindowHelper
    {
        #region Fields

        private static IVsOutputWindowPane _giteaVSOutputWindowPane;

        #endregion Fields

        #region Properties

        private static IVsOutputWindowPane GiteaVSOutputWindowPane =>
            _giteaVSOutputWindowPane ?? (_giteaVSOutputWindowPane = GetGitLabVsOutputWindowPane());

        #endregion Properties

        #region Methods

        internal static void DiagnosticWriteLine(string message, Exception ex = null)
        {
            if (ex != null)
            {
                message += $": {ex}";
            }

            WriteLine("Diagnostic", message);
        }

        internal static void ExceptionWriteLine(string message, Exception ex)
        {
            var exceptionMessage = $"{message}: {ex}";
            WriteLine("Handled Exception", exceptionMessage);
        }

        internal static void WarningWriteLine(string message)
        {
            WriteLine("Warning", message);
        }

        private static IVsOutputWindowPane GetGitLabVsOutputWindowPane()
        {
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null) return null;

            Guid outputPaneGuid = new Guid(PackageGuids.guidGitea4VSCmdSet.ToByteArray());
            IVsOutputWindowPane windowPane;

            outputWindow.CreatePane(ref outputPaneGuid, "Gitea for Visual Studio", 1, 1);
            outputWindow.GetPane(ref outputPaneGuid, out windowPane);

            return windowPane;
        }

        private static void WriteLine(string category, string message)
        {
            var outputWindowPane = GiteaVSOutputWindowPane;
            if (outputWindowPane != null)
            {
                string outputMessage = $"[Gitea for Visual Studio  {category} {DateTime.Now.ToString("hh:mm:ss tt")}] {message}{Environment.NewLine}";
                outputWindowPane.OutputString(outputMessage);
            }
        }
      
        #endregion Methods
    }
}