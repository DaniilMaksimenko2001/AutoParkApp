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
using System.Data.Entity;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.IO;

namespace AutoParkApp
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private bool _loading;

        public MainPage() { InitializeComponent(); }

        private void Page_Loaded(object sender, RoutedEventArgs e) => LoadAll();

        private void LoadAll()
        {
            _loading = true;
            using (var db = new AutoParkDBEntities())
            {
                // обновление статусов ТС по завершённым рейсам
                var now = DateTime.Now;
                foreach (var v in db.Vehicles.Where(x => x.Status == "В рейсе").ToList())
                {
                    bool active = db.Routes.Any(r => r.VehicleId == v.Id && r.StartDate <= now && r.EndDate >= now);
                    if (!active) v.Status = "Доступен";
                }
                db.SaveChanges();

                var vehicles = db.Vehicles.OrderBy(v => v.LicensePlate).ToList();
                var drivers = db.Drivers.OrderBy(d => d.FullName).ToList();

                // комбобоксы-фильтры 
                var vf = new List<Vehicle> { new Vehicle { Id = 0, LicensePlate = "Все ТС" } };
                vf.AddRange(vehicles);
                cbVehicleFilter.ItemsSource = vf;
                cbVehicleFilter.SelectedIndex = 0;

                var df = new List<Driver> { new Driver { Id = 0, FullName = "Все водители" } };
                df.AddRange(drivers);
                cbDriverFilter.ItemsSource = df;
                cbDriverFilter.SelectedIndex = 0;
            }
            _loading = false;
            LoadVehicles();
            LoadDrivers();
            LoadRoutes();
            LoadReport();
        }

        // ---------- ТРАНСПОРТ ----------
        private void LoadVehicles()
        {
            using (var db = new AutoParkDBEntities())
            {
                IQueryable<Vehicle> q = db.Vehicles;
                switch (cbSort.SelectedIndex)
                {
                    case 1: q = q.OrderBy(v => v.Mileage); break;
                    case 2: q = q.OrderByDescending(v => v.Mileage); break;
                    case 3: q = q.OrderBy(v => v.YearOfManufacture); break;
                    case 4: q = q.OrderByDescending(v => v.YearOfManufacture); break;
                    case 5: q = q.OrderBy(v => v.Model); break;
                    default: q = q.OrderBy(v => v.Id); break;
                }
                dgVehicles.ItemsSource = q.ToList();
            }
        }

        // Когда пользователь выбирает другой пункт в комбобоксе сортировки, список машин загружается заново
        private void Sort_Changed(object s, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadVehicles();
        }

        private void AddVehicle_Click(object s, RoutedEventArgs e)
        {
            NavigationService.Navigate(new VehicleEditPage(new Vehicle { Status = "Доступен" }));
        }

        private void EditVehicle_Click(object s, RoutedEventArgs e)
        {
            if (!(dgVehicles.SelectedItem is Vehicle v)) { MessageBox.Show("Выберите ТС"); return; }
            NavigationService.Navigate(new VehicleEditPage(v));
        }

        private void DeleteVehicle_Click(object s, RoutedEventArgs e)
        {
            if (!(dgVehicles.SelectedItem is Vehicle sel)) { MessageBox.Show("Выберите ТС"); return; }
            if (MessageBox.Show($"Удалить {sel.LicensePlate}?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            using (var db = new AutoParkDBEntities())
            {
                if (db.Routes.Any(r => r.VehicleId == sel.Id))
                {
                    MessageBox.Show("Нельзя удалить: у ТС есть рейсы.");
                    return;
                }
                var v = db.Vehicles.Find(sel.Id);
                db.Vehicles.Remove(v);
                db.SaveChanges();
            }
            LoadAll();
        }

        private void VehicleHistory_Click(object s, RoutedEventArgs e)
        {
            if (!(dgVehicles.SelectedItem is Vehicle v)) { MessageBox.Show("Выберите ТС"); return; }
            ResetFilters();
            cbVehicleFilter.SelectedValue = v.Id;
            Tabs.SelectedIndex = 2;
        }

        // ---------- ВОДИТЕЛИ ----------
        private void LoadDrivers()
        {
            using (var db = new AutoParkDBEntities()) dgDrivers.ItemsSource = db.Drivers.OrderBy(d => d.FullName).ToList();
        }

        private void AddDriver_Click(object s, RoutedEventArgs e)
        {
            NavigationService.Navigate(new DriverEditPage(new Driver()));
        }

        private void EditDriver_Click(object s, RoutedEventArgs e)
        {
            if (!(dgDrivers.SelectedItem is Driver d)) { MessageBox.Show("Выберите водителя"); return; }
            NavigationService.Navigate(new DriverEditPage(d));
        }

        private void DeleteDriver_Click(object s, RoutedEventArgs e)
        {
            if (!(dgDrivers.SelectedItem is Driver sel)) { MessageBox.Show("Выберите водителя"); return; }
            if (MessageBox.Show($"Удалить {sel.FullName}?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            using (var db = new AutoParkDBEntities())
            {
                if (db.Routes.Any(r => r.DriverId == sel.Id))
                {
                    MessageBox.Show("Нельзя удалить: у водителя есть рейсы.");
                    return;
                }
                db.Drivers.Remove(db.Drivers.Find(sel.Id));
                db.SaveChanges();
            }
            LoadAll();
        }

        private void DriverHistory_Click(object s, RoutedEventArgs e)
        {
            if (!(dgDrivers.SelectedItem is Driver d)) { MessageBox.Show("Выберите водителя"); return; }
            ResetFilters();
            cbDriverFilter.SelectedValue = d.Id;
            Tabs.SelectedIndex = 2;
        }

        // ---------- РЕЙСЫ ----------
        private void LoadRoutes()
        {
            if (_loading) return;
            using (var db = new AutoParkDBEntities())
            {
                IQueryable<Route> q = db.Routes.Include(r => r.Vehicle).Include(r => r.Driver);

                if (dpFrom.SelectedDate is DateTime from) q = q.Where(r => r.StartDate >= from);
                if (dpTo.SelectedDate is DateTime to)
                {
                    var end = to.Date.AddDays(1);
                    q = q.Where(r => r.StartDate < end);
                }
                // Pattern Matching - проверка и объявление переменной в одном выражении
                if (cbVehicleFilter.SelectedValue is int vid && vid != 0) q = q.Where(r => r.VehicleId == vid);
                if (cbDriverFilter.SelectedValue is int did && did != 0) q = q.Where(r => r.DriverId == did);
                dgRoutes.ItemsSource = q.OrderByDescending(r => r.StartDate).ToList();
            }
        }

        private void Filter_Changed(object s, EventArgs e) { if (IsLoaded) LoadRoutes(); } // пока страница не загружена, игнорируем событие

        private void ResetFilters()
        {
            _loading = true;
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;
            cbVehicleFilter.SelectedIndex = 0;
            cbDriverFilter.SelectedIndex = 0;
            _loading = false;
        }

        private void ResetFilters_Click(object s, RoutedEventArgs e)
        {
            ResetFilters();
            LoadRoutes();
        }

        private void AddRoute_Click(object s, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RouteEditPage());
        }

        // ---------- ОТЧЁТ ----------
        public class ReportRow
        {
            public string LicensePlate { get; set; }
            public int Mileage { get; set; }
            public int RoutesCount { get; set; }
            public int TotalDistance { get; set; }
        }
        private void LoadReport()
        {
            using (var db = new AutoParkDBEntities())
            {
                dgReport.ItemsSource = db.Vehicles
                    .Select(v => new ReportRow
                    {
                        LicensePlate = v.LicensePlate,
                        Mileage = v.Mileage,
                        RoutesCount = v.Routes.Count(),
                        TotalDistance = v.Routes.Sum(r => (int?)r.Distance) ?? 0
                    })
                    .OrderByDescending(x => x.TotalDistance)
                    .ToList();
            }
        }
        private void CreateReport(object sender, RoutedEventArgs e)
        {
            var rows = dgReport.Items.OfType<ReportRow>().ToList();
            if (rows.Count == 0)
            {
                MessageBox.Show("Данных для формирования отчета нет!");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Сохранение отчета",
                Filter = "Excel Worksheets (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                FileName = $"Mileage_{DateTime.Now:dd-MM-yyyy_HH-mm}"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var sheet = workbook.AddWorksheet("Пробег");

                    sheet.Range("A1:E1").Merge();
                    sheet.Cell("A1").Value = "Отчет по суммарному пробегу транспортных средств";
                    sheet.Cell("A1").Style.Font.Bold = true;
                    sheet.Cell("A1").Style.Font.FontSize = 16;
                    sheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    sheet.Cell("A2").Value = "Дата:";
                    sheet.Cell("B2").Value = DateTime.Now;
                    sheet.Cell("B2").Style.DateFormat.Format = "dd.MM.yyyy HH:mm";

                    sheet.Cell(4, 1).Value = "№";
                    sheet.Cell(4, 2).Value = "Гос. номер";
                    sheet.Cell(4, 3).Value = "Рейсов";
                    sheet.Cell(4, 4).Value = "Пробег по рейсам, км";
                    sheet.Cell(4, 5).Value = "Пробег на одометре, км";
                    sheet.Range("A4:E4").Style.Font.Bold = true;

                    int row = 5;
                    int number = 1;
                    foreach (var item in rows)
                    {
                        sheet.Cell(row, 1).Value = number;
                        sheet.Cell(row, 2).Value = item.LicensePlate ?? "";
                        sheet.Cell(row, 3).Value = item.RoutesCount;
                        sheet.Cell(row, 4).Value = item.TotalDistance;
                        sheet.Cell(row, 5).Value = item.Mileage;
                        row++;
                        number++;
                    }

                    int sumRow = row + 1;
                    sheet.Cell(sumRow, 1).Value = "Всего ТС:";
                    sheet.Cell(sumRow, 2).Value = rows.Count;
                    sheet.Cell(sumRow, 3).Value = rows.Sum(r => r.RoutesCount);
                    sheet.Cell(sumRow, 4).Value = rows.Sum(r => r.TotalDistance);
                    sheet.Cell(sumRow, 5).Value = rows.Sum(r => r.Mileage);
                    sheet.Range(sumRow, 1, sumRow, 5).Style.Font.Bold = true;

                    sheet.Columns().AdjustToContents();

                    workbook.SaveAs(dialog.FileName);
                    MessageBox.Show("Отчет успешно сформирован!");
                }
            }
            catch (IOException)
            {
                MessageBox.Show("Не удалось сохранить файл, возможно он открыт в Excel");
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Нет прав на запись в выбранную папку");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
