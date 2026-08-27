namespace LLOIS.Views;

using System.Windows;

public partial class UpdateProgressWindow : Window
{
    public UpdateProgressWindow()
    {
        InitializeComponent();
    }

    public void SetProgress(double percent)
    {
        ProgressBarControl.IsIndeterminate = false;
        ProgressBarControl.Value = percent;
        PercentText.Text = $"{percent:0}%";
    }

    public void SetIndeterminate()
    {
        ProgressBarControl.IsIndeterminate = true;
        PercentText.Text = "";
    }

    public void SetStatus(string text)
    {
        StatusText.Text = text;
    }
}