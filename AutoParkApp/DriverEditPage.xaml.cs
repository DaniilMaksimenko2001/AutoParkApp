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
    /// Interaction logic for DriverEditPage.xaml
    /// </summary>
    public partial class DriverEditPage : Page
    {
        public DriverEditPage(Driver d)
        {
            InitializeComponent();
            tbTitle.Text = d.Id == 0 ? "Новый водитель" : "Редактирование водителя";

            DataContext = new Driver
            {
                Id = d.Id,
                FullName = d.FullName,
                LicenseNumber = d.LicenseNumber,
                PhoneNumber = d.PhoneNumber,
                Experience = d.Experience,
                Email = d.Email,
                Password = d.Password
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var d = (Driver)DataContext;
            var err = new StringBuilder();

            if (string.IsNullOrWhiteSpace(d.FullName)) err.AppendLine("Укажите ФИО.");
            if (string.IsNullOrWhiteSpace(d.LicenseNumber)) err.AppendLine("Укажите номер ВУ.");
            if (string.IsNullOrWhiteSpace(d.PhoneNumber) || !Regex.IsMatch(d.PhoneNumber, @"^\+?\d{10,15}$"))
                err.AppendLine("Телефон: 10–15 цифр, можно с «+».");
            if (d.Experience < 0 || d.Experience > 60) err.AppendLine("Стаж: от 0 до 60.");
            if (string.IsNullOrWhiteSpace(d.Email) || !Regex.IsMatch(d.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                err.AppendLine("Некорректный email.");
            if (string.IsNullOrWhiteSpace(d.Password) || d.Password.Length < 4)
                err.AppendLine("Пароль: минимум 4 символа.");

            if (err.Length > 0) { MessageBox.Show(err.ToString(), "Ошибки"); return; }

            using (var db = new AutoParkDBEntities())
            {
                if (db.Drivers.Any(x => (x.Email == d.Email || x.LicenseNumber == d.LicenseNumber) && x.Id != d.Id))
                {
                    MessageBox.Show("Водитель с таким email или номером ВУ уже существует.");
                    return;
                }

                if (d.Id == 0) db.Drivers.Add(d);
                else
                {
                    var x = db.Drivers.Find(d.Id);
                    x.FullName = d.FullName;
                    x.LicenseNumber = d.LicenseNumber;
                    x.PhoneNumber = d.PhoneNumber;
                    x.Experience = d.Experience;
                    x.Email = d.Email;
                    x.Password = d.Password;
                }
                db.SaveChanges();
            }
            NavigationService.GoBack();
        }

        private void Back_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    }
}
