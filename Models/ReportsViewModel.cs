namespace FamilyHub.Models;

public class ReportsViewModel
{
    public int TotalMembers { get; set; }
    public int MaleMembers { get; set; }
    public int FemaleMembers { get; set; }
    public int Children { get; set; }
    public int Adults { get; set; }
    public int Seniors { get; set; }
    public int TotalRelationships { get; set; }
    public int Families { get; set; }
    public int Users { get; set; }
    public int Administrators { get; set; }
    public int TotalActivities { get; set; }
    public IReadOnlyList<ActivityLog> RecentLogs { get; set; } = Array.Empty<ActivityLog>();
    public IReadOnlyList<ReportMetric> MembersByGender { get; set; } = Array.Empty<ReportMetric>();
    public IReadOnlyList<ReportMetric> AgeDistribution { get; set; } = Array.Empty<ReportMetric>();
    public IReadOnlyList<ReportMetric> MonthlyRegistrations { get; set; } = Array.Empty<ReportMetric>();
    public IReadOnlyList<ReportMetric> RelationshipsByType { get; set; } = Array.Empty<ReportMetric>();
    public IReadOnlyList<ReportMetric> UserActivity { get; set; } = Array.Empty<ReportMetric>();
}

public class ReportMetric
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}
