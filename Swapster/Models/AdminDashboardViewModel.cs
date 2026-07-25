namespace Swapster.Models
{
    public class AdminDashboardViewModel
    {
        public string AdminName { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
        public int ActiveSwaps { get; set; }
        public int TotalSkills { get; set; }
        public int PendingReportsCount { get; set; }
    }
}