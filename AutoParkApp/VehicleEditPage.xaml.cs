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
    /// Interaction logic for VehicleEditPage.xaml
    /// </summary>
    public partial class VehicleEditPage : Page
    {
        public VehicleEditPage(Vehicle vehicle)
        {
            InitializeComponent();
            tbTitle.Text = vehicle.Id == 0 ? "Новое ТС" : "Редактирование ТС";

            DataContext = new Vehicle
            {
                Id = vehicle.Id,
                LicensePlate = vehicle.LicensePlate,
                Model = vehicle.Model,
                Manufacturer = vehicle.Manufacturer,
                YearOfManufacture = vehicle.YearOfManufacture,
                Mileage = vehicle.Mileage,
                Status = vehicle.Status
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var v = (Vehicle)DataContext;
            var err = new StringBuilder();

            if (string.IsNullOrWhiteSpace(v.LicensePlate) || !Vehicle.PlateRegex.IsMatch(v.LicensePlate)) err.AppendLine("Гос. номер: формат А123ВС77 (русские буквы).");
            if (string.IsNullOrWhiteSpace(v.Model)) err.AppendLine("Укажите модель.");
            if (string.IsNullOrWhiteSpace(v.Manufacturer)) err.AppendLine("Укажите производителя.");
            if (v.YearOfManufacture < 1980 || v.YearOfManufacture > DateTime.Now.Year) err.AppendLine($"Год выпуска: от 1980 до {DateTime.Now.Year}.");
            if (v.Mileage < 0) err.AppendLine("Пробег не может быть отрицательным.");

            if (err.Length > 0) { MessageBox.Show(err.ToString(), "Ошибки"); return; }

            using (var db = new AutoParkDBEntities())
            {
                if (db.Vehicles.Any(x => x.LicensePlate == v.LicensePlate && x.Id != v.Id))
                {
                    MessageBox.Show("ТС с таким номером уже есть.");
                    return;
                }

                if (v.Id == 0) db.Vehicles.Add(v);
                else
                {
                    var dbV = db.Vehicles.Find(v.Id);
                    dbV.LicensePlate = v.LicensePlate;
                    dbV.Model = v.Model;
                    dbV.Manufacturer = v.Manufacturer;
                    dbV.YearOfManufacture = v.YearOfManufacture;
                    dbV.Mileage = v.Mileage;
                    dbV.Status = v.Status;
                }
                db.SaveChanges();
            }
            NavigationService.GoBack();
        }

        private void Back_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    }
}
