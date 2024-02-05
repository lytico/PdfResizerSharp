using System;
using Gtk;

namespace PdfResizer
{
    class Program
    {
        public static Application App;
        public static Window Win;

        [STAThread]
        public static void Main(string[] args)
        {
            Application.Init();

            App = new Application("org.Samples.Samples", GLib.ApplicationFlags.None);
            App.Register(GLib.Cancellable.Current);

            Win = new MainWindow();
            App.AddWindow(Win);

            var menu = new GLib.Menu();
            menu.AppendItem(new GLib.MenuItem("Help", "app.help"));
            menu.AppendItem(new GLib.MenuItem("About", "app.about"));
            menu.AppendItem(new GLib.MenuItem("Quit", "app.quit"));
            App.AppMenu = menu;

            var helpAction = new GLib.SimpleAction("help", null);
            helpAction.Activated += HelpActivated;
            App.AddAction(helpAction);

            var aboutAction = new GLib.SimpleAction("about", null);
            aboutAction.Activated += AboutActivated;
            App.AddAction(aboutAction);

            var quitAction = new GLib.SimpleAction("quit", null);
            quitAction.Activated += QuitActivated;
            App.AddAction(quitAction);

            Win.ShowAll();
            Application.Run();
        }

        private static void HelpActivated(object sender, EventArgs e)
        {

        }

        private static void AboutActivated(object sender, EventArgs e)
        {
            var dialog = new AboutDialog
            {
                TransientFor = Win,
                ProgramName = "Pdf Resizer",
                Version = "1.0.0.0",
                Comments = "An application to resize pdf files with ghostscript",
                LogoIconName = "system-run-symbolic",
                License = "This sample application is licensed with AGPL",
                Website = "https://www.github.com/lytico/PdfResizerSharp",
                WebsiteLabel = "PdfResizerSharp Website"
            };
            dialog.Run();
            dialog.Hide();
        }

        private static void QuitActivated(object sender, EventArgs e)
        {
            Application.Quit();
        }
    }
}
