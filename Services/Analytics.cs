using CollegeEventManagement.Models;

namespace CollegeEventManagement.Services
{
    // Small helpers that turn registrations into chart data. Everything is computed from real database rows.
    public static class Analytics
    {
        public static int Percent(int part, int whole) => whole == 0 ? 0 : (int)Math.Round(100.0 * part / whole);

        // Last N months: A = registrations made that month, B = check-ins recorded that month
        public static ChartSeries Monthly(IEnumerable<Registration> regs, int months = 6)
        {
            var list = regs.ToList();
            var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(months - 1));
            var s = new ChartSeries();
            for (int i = 0; i < months; i++)
            {
                var m = first.AddMonths(i);
                s.Labels.Add(m.ToString("MMM"));
                s.A.Add(list.Count(r => r.RegisteredOn.Year == m.Year && r.RegisteredOn.Month == m.Month));
                s.B.Add(list.Count(r => r.Attendance != null && r.Attendance.CheckedInAt.Year == m.Year && r.Attendance.CheckedInAt.Month == m.Month));
            }
            return s;
        }

        public static string Short(string text, int max = 18) => text.Length <= max ? text : text.Substring(0, max - 1).TrimEnd() + "…";
    }
}
