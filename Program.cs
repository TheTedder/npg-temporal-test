using Microsoft.EntityFrameworkCore;

namespace NpgTemporalTest;

class Program
{
    static async Task Main(string[] args)
    {
        using var context = new EmployeeContext();
        List<Employee> employees = await context.Employees.ToListAsync();

        int arbitraryEmployee = await context.Employees.Select(emp => emp.EmployeeId).FirstAsync();

        List<Employee> employeeHistory = 
            await context.Employees
                .IncludeHistory()
                .Where(emp => emp.EmployeeId == arbitraryEmployee)
                .OrderBy(emp => emp.ValidPeriod.LowerBound)
                .ToListAsync();
    }
}
