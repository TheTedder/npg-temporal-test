using NpgsqlTypes;

namespace NpgTemporalTest;

public class Employee
{
    public int EmployeeId { get; set; }
    public string Name { get; set; }
    public string Department { get; set; }
    public decimal Salary { get; set; }
    public NpgsqlRange<DateTime> ValidPeriod { get; set; }
}