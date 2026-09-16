namespace TaskManagement.Application.DTOs.Dashboard;

public class DashboardSummaryResponse
{
    public int TotalTasks { get; set; }

    public int ToDoCount { get; set; }

    public int InProgressCount { get; set; }

    public int DoneCount { get; set; }

    public int HighPriorityCount { get; set; }

    public int OverdueCount { get; set; }

    public int DueTodayCount { get; set; }

    public int UpcomingCount { get; set; }
}

