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
    /// Interaction logic for RouteEditPage.xaml
    /// </summary>
    public partial class RouteEditPage : Page
    {
        public RouteEditPage()
        {
            InitializeComponent();
            using (var db = new AutoParkDBEntities())
            {
                cbVehicle.ItemsSource = db.Vehicles.Where(v => v.Status == "Доступен").ToList();
                cbDriver.ItemsSource = db.Drivers.OrderBy(d => d.FullName).ToList();
            }
            dpStart.SelectedDate = DateTime.Today;
            dpEnd.SelectedDate = DateTime.Today;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var err = new StringBuilder();

            if (cbVehicle.SelectedValue == null) err.AppendLine("Выберите ТС.");
            if (cbDriver.SelectedValue == null) err.AppendLine("Выберите водителя.");
            if (string.IsNullOrWhiteSpace(tbFrom.Text)) err.AppendLine("Укажите пункт отправления.");
            if (string.IsNullOrWhiteSpace(tbTo.Text)) err.AppendLine("Укажите пункт назначения.");
            if (!int.TryParse(tbDistance.Text, out int dist) || dist <= 0)
                err.AppendLine("Расстояние — положительное целое число.");

            DateTime start = default, end = default;
            if (!dpStart.SelectedDate.HasValue || !TimeSpan.TryParse(tbStartTime.Text, out var ts)) err.AppendLine("Некорректная дата/время начала.");
            else start = dpStart.SelectedDate.Value.Date + ts;

            if (!dpEnd.SelectedDate.HasValue || !TimeSpan.TryParse(tbEndTime.Text, out var te)) {err.AppendLine("Некорректная дата/время окончания.");} else end = dpEnd.SelectedDate.Value.Date + te;

            if (err.Length == 0 && end <= start) err.AppendLine("Окончание должно быть позже начала.");

            if (err.Length > 0) { MessageBox.Show(err.ToString(), "Ошибки"); return; }

            int vid = (int)cbVehicle.SelectedValue;
            int did = (int)cbDriver.SelectedValue;

            using (var db = new AutoParkDBEntities())
            // Открываем транзакцию. До вызова commit().
            using (var tx = db.Database.BeginTransaction())
            {
                var vehicle = db.Vehicles.Find(vid);
                if (vehicle.Status != "Доступен")
                {
                    MessageBox.Show("ТС уже недоступно.");
                    return;
                }

                bool driverBusy = db.Routes.Any(r => r.DriverId == did && r.StartDate < end && r.EndDate > start);
                if (driverBusy)
                {
                    MessageBox.Show("Водитель уже занят в этот период.");
                    return;
                }

                db.Routes.Add(new Route
                {
                    VehicleId = vid,
                    DriverId = did,
                    StartDate = start,
                    EndDate = end,
                    StartLocation = tbFrom.Text.Trim(),
                    EndLocation = tbTo.Text.Trim(),
                    Distance = dist
                });

                vehicle.Status = "В рейсе";
                vehicle.Mileage += dist;

                db.SaveChanges();
                tx.Commit();
            }
            NavigationService.GoBack();
        }

        private void Back_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    }
}
