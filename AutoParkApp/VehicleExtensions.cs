using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoParkApp
{
    public partial class Vehicle
    {
        public const int ServiceInterval = 10000;

        // бейдж ТО: пробег достиг порога
        public bool NeedsService => Mileage >= ServiceInterval;
        public string ServiceBadge => NeedsService ? "🔧 Нужно ТО" : "";

        // формат: А123ВС77 или А123ВС777 
        public static readonly Regex PlateRegex = new Regex(@"^[АВЕКМНОРСТУХ]\d{3}[АВЕКМНОРСТУХ]{2}\d{2,3}$");
    }
}
