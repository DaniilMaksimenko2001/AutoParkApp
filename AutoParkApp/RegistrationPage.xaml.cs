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
using System.Text.RegularExpressions;

namespace AutoParkApp
{
    /// <summary>
    /// Interaction logic for RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : Page
    {
        public RegistrationPage()
        {
            InitializeComponent();
        }

        private void OpenLoginPage(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginPage());
        }

        private void SaveUserClick(object sender, RoutedEventArgs e)
        {
            string name = FullNameBox.Text.Trim();
            string licenseNumber = LicenseNumberBox.Text.Trim();
            string phoneNumber = PhoneNumberBox.Text.Trim();
            string experienceText = ExperienceBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Text.Trim();
            string repeatPassword = PasswordRepeatBox.Text.Trim();

            var errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(name))
                errors.AppendLine("Укажите ФИО.");
            if (string.IsNullOrWhiteSpace(licenseNumber))
                errors.AppendLine("Укажите номер водительского удостоверения.");
            if (!Regex.IsMatch(phoneNumber, @"^\+?\d{10,15}$"))
                errors.AppendLine("Телефон: 10–15 цифр, можно с «+».");
            if (!int.TryParse(experienceText, out int experience) || experience < 0 || experience > 60)
                errors.AppendLine("Стаж: целое число от 0 до 60.");
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.AppendLine("Некорректный email.");
            if (password.Length < 4)
                errors.AppendLine("Пароль: минимум 4 символа.");
            if (password != repeatPassword)
                errors.AppendLine("Пароли не совпадают.");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибки", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new AutoParkDBEntities())
            {
                if (db.Drivers.Any(d => d.Email == email || d.LicenseNumber == licenseNumber))
                {
                    MessageBox.Show("Водитель с таким email или номером ВУ уже зарегистрирован.");
                    return;
                }

                var driver = new Driver
                {
                    FullName = name,
                    LicenseNumber = licenseNumber,
                    PhoneNumber = phoneNumber,
                    Experience = experience,
                    Email = email,
                    Password = password
                };

                db.Drivers.Add(driver);
                db.SaveChanges(); 
            }

            MessageBox.Show("Регистрация прошла успешно!");
            NavigationService.Navigate(new LoginPage());
        }
    }
}
