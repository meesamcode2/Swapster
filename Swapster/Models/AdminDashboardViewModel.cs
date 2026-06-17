namespace Swapster.Models
{
    public class AdminDashboardViewModel
    {
        public string AdminName { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveSwaps { get; set; }
        public int TotalSkills { get; set; }
        public int PendingReportsCount { get; set; }

    }
}