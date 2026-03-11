using DevExpress.Xpf.Core;
using LibraryAutomation.Data;
using System;
using System.Linq;
using System.Windows;

namespace LibraryAutomationn
{
    public partial class LoginView : ThemedWindow
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new LibraryDbContext())
            {
                var user = db.Users.FirstOrDefault(u => u.Username==txtUser.Text);
                if (user != null && user.IsBanned)
                {
                    MessageBox.Show("Üzgünüz, hesabınız yönetici tarafından askıya alınmıştır.", "BANLANDINIZ");
                    return; 
                }
                if (txtUser.Text == "admin" && txtPass.Password == "Admin")
                {
                    MainWindow main = new MainWindow("Admin");
                    main.btnAdminPanel.Visibility = Visibility.Visible;
                    main.AdminSeparator.Visibility = Visibility.Visible;
                    main.Show();
                    this.Close();
                }
                else if (txtUser.Text == "user" && txtPass.Password == "user")
                {
                    MainWindow main = new MainWindow("User");
                    main.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Kullanıcı adı veya şifre hatalı!");
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}