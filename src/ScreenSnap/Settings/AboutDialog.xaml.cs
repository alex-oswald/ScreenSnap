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

        // The About dialog renders the icon at 48 DIPs, so we load the 256x256
        // PNG (rather than the multi-frame .ico, where BitmapImage tends to pick
        // a small frame and scale it up, producing a blurry image).
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ScreenSnap.png");
        if (File.Exists(iconPath))
            AppIcon.Source = new BitmapImage(new Uri(iconPath));
    }
}
