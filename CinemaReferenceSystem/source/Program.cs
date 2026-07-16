using CinemaReferenceSystem.Forms;
using CinemaReferenceSystem.Services;

namespace CinemaReferenceSystem;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var db = new DatabaseService(DatabaseConfig.ConnectionString);
        var authService = new AuthService(db);

        while (true)
        {
            using var login = new LoginForm(authService);
            if (login.ShowDialog() != DialogResult.OK || login.CurrentUser == null)
                break;

            using var mainForm = new MainForm(login.CurrentUser, db);
            Application.Run(mainForm);

            if (!mainForm.IsLogout)
                break;
        }
    }
}