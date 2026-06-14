using System;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ScreenSnap.Settings;

/// <summary>
/// Small "About" surface showing the app icon, version, author, description, and a link to
/// the project's GitHub page. Shown as a dialog from the settings window.
/// </summary>
public sealed partial class AboutDialog : ContentDialog
{
    public AboutDialog()
    {
        InitializeComponent();

        VersionText.Text = $"Version {AppInfo.Version}";
        AuthorText.Text = $"By {AppInfo.Author}";
        DescriptionText.Text = AppInfo.Description;
        GitHubLink.NavigateUri = new Uri(AppInfo.GitHubUrl);

        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ScreenSnap.ico");
        if (File.Exists(iconPath))
            AppIcon.Source = new BitmapImage(new Uri(iconPath));
    }
}
