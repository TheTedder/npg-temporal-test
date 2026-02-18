// using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;

namespace NpgTemporalTest;

class Program
{
    static async Task Main(string[] args)
    {
        using var context = new EmployeeContext();
        List<Employee> employees = await context.Employees.ToListAsync();
    }
}

