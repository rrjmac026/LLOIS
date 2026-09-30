namespace LLOIS.Views;

using System.Windows;
using System.Windows.Controls;
using LLOIS.Services;

public partial class EditUserDialog : Window
{
    private readonly ApiUserRole _originalRole;

    public int UserId { get; }
    public string UpdatedUsername { get; private set; } = "";
    public ApiUserRole UpdatedRole { get; private set; }

    public EditUserDialog(ApiUserSummary user)
    {
        InitializeComponent();

        UserId = user.Id;
        _originalRole = user.Role;
        UpdatedRole = user.Role;
        UsernameBox.Text = user.Username;

        // Select by name, not by position. If the user's role isn't in the combo
        // (e.g. SuperAdmin), it stays empty instead of showing "Viewer" — the old code
        // did the latter, so saving would have quietly demoted a SuperAdmin.
        RoleCombo.SelectedItem = RoleCombo.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(i => i.Content?.ToString() == user.Role.ToString());
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(UsernameBox.Text))
        { ShowErr("Username is required."); return; }

        UpdatedUsername = UsernameBox.Text.Trim();

        // Nothing selected (role isn't in the combo) -> keep the role they already had.
        UpdatedRole = (RoleCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "SuperAdmin" => ApiUserRole.SuperAdmin,
            "Admin"      => ApiUserRole.Admin,
            "Encoder"    => ApiUserRole.Encoder,
            "Viewer"     => ApiUserRole.Viewer,
            _            => _originalRole
        };

        DialogResult = true;
    }

    private void ShowErr(string msg)
    {
        ErrText.Text = msg;
        ErrText.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}