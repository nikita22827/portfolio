using CinemaReferenceSystem.Models;
using CinemaReferenceSystem.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CinemaReferenceSystem.Forms;

public partial class LoginForm : Form
{
    private readonly AuthService _authService;
    public User? CurrentUser { get; private set; }

    public LoginForm(AuthService authService)
    {
        _authService = authService;
        InitializeComponent();
    }

    private async void btnLogin_Click(object sender, EventArgs e)
    {
        var user = await _authService.LoginAsync(txtUsername.Text.Trim(), txtPassword.Text);
        if (user != null)
        {
            CurrentUser = user;
            DialogResult = DialogResult.OK;
            Close();
        }
        else
            MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private async void btnRegister_Click(object sender, EventArgs e)
    {
        string password = txtPassword.Text;

        if (password.Length < 8)
        {
            MessageBox.Show("Пароль должен содержать не менее 8 символов.",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var success = await _authService.RegisterAsync(txtUsername.Text.Trim(), password);
        MessageBox.Show(success ? "✅ Регистрация успешна!" : "❌ Не удалось зарегистрироваться",
                       success ? "Успех" : "Ошибка", MessageBoxButtons.OK,
                       success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    private void InitializeComponent()
    {
        this.Text = "Кинотеатры — Вход";
        this.Size = new Size(460, 340);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(240, 240, 245);

        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40) };

        var lblTitle = new Label { Text = "Система справочной службы кинотеатров", Font = new Font("Segoe UI", 14, FontStyle.Bold), AutoSize = true, Location = new Point(20, 20) };

        var lblUser = new Label { Text = "Логин:", Location = new Point(30, 90), Width = 80, Font = new Font("Segoe UI", 10) };
        txtUsername = new TextBox { Location = new Point(120, 88), Width = 260, Font = new Font("Segoe UI", 10) };

        var lblPass = new Label { Text = "Пароль:", Location = new Point(30, 140), Width = 80, Font = new Font("Segoe UI", 10) };
        txtPassword = new TextBox { Location = new Point(120, 138), Width = 260, PasswordChar = '*', Font = new Font("Segoe UI", 10) };

        btnLogin = new Button { Text = "Войти", Location = new Point(120, 200), Width = 110, Height = 40, Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.DodgerBlue, ForeColor = Color.White };
        btnRegister = new Button { Text = "Зарегистрироваться", Location = new Point(250, 200), Width = 145, Height = 40, Font = new Font("Segoe UI", 10) };

        btnLogin.Click += btnLogin_Click;
        btnRegister.Click += btnRegister_Click;

        panel.Controls.AddRange(new Control[] { lblTitle, lblUser, txtUsername, lblPass, txtPassword, btnLogin, btnRegister });
        this.Controls.Add(panel);
    }

    private TextBox txtUsername = new();
    private TextBox txtPassword = new();
    private Button btnLogin = new();
    private Button btnRegister = new();
}