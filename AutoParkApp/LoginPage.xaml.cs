using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoParkApp
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void OpenRegistrationPage(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegistrationPage());
        }

        private void LoginClick(object sender, RoutedEventArgs e)
        {
            string email = LoginBox.Text.Trim();
            string password = PasswordBox.Password; 

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите email и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new AutoParkDBEntities())
            {
                var driver = db.Drivers.FirstOrDefault(d => d.Email == email && d.Password == password);

                if (driver == null)
                {
                    MessageBox.Show("Неверный email или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                CurrentUser.Driver = driver;
            }
            NavigationService.Navigate(new MainPage());
        }
    }
    public static class CurrentUser
    {
        public static Driver Driver { get; set; }
    }
}
