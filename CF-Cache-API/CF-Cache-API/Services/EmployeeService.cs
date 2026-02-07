using CF_Cache_API.Models;

namespace CF_Cache_API.Services;

public class EmployeeService
{
    private readonly List<Employee> _employees;

    public EmployeeService()
    {
        _employees = GenerateEmployees();
    }

    public IEnumerable<Employee> GetAll(string tenantId, int page = 1, int pageSize = 10)
    {
        return _employees.Where(e => e.TenantId == tenantId)
            .Skip((page - 1) * pageSize).Take(pageSize);
    }

    public (IEnumerable<Employee>, int) SearchByFirstName(string tenantId, string firstName, int page = 1, int pageSize = 10)
    {
        var query = _employees.Where(e => e.TenantId == tenantId && e.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase));
        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public (IEnumerable<Employee>, int) SearchByLastName(string tenantId, string lastName, int page = 1, int pageSize = 10)
    {
        var query = _employees.Where(e => e.TenantId == tenantId && e.LastName.Contains(lastName, StringComparison.OrdinalIgnoreCase));
        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public (IEnumerable<Employee>, int) SearchByCompany(string tenantId, string companyName, int page = 1, int pageSize = 10)
    {
        var query = _employees.Where(e => e.TenantId == tenantId && e.CompanyName.Contains(companyName, StringComparison.OrdinalIgnoreCase));
        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public (IEnumerable<Employee>, int) SearchByPosition(string tenantId, string position, int page = 1, int pageSize = 10)
    {
        var query = _employees.Where(e => e.TenantId == tenantId && e.Position.Contains(position, StringComparison.OrdinalIgnoreCase));
        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public (IEnumerable<Employee>, int) Search(string tenantId, string? firstName, string? lastName, string? companyName, string? position, int page = 1, int pageSize = 10)
    {
        var query = _employees.Where(e => e.TenantId == tenantId).AsEnumerable();

        if (!string.IsNullOrEmpty(firstName))
            query = query.Where(e => e.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(lastName))
            query = query.Where(e => e.LastName.Contains(lastName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(companyName))
            query = query.Where(e => e.CompanyName.Contains(companyName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(position))
            query = query.Where(e => e.Position.Contains(position, StringComparison.OrdinalIgnoreCase));

        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public int GetTotalCount(string tenantId) => _employees.Count(e => e.TenantId == tenantId);

    public Employee AddEmployee(string tenantId, string firstName, string lastName, string companyName, string position)
    {
        var newId = _employees.Any() ? _employees.Max(e => e.Id) + 1 : 1;
        var employee = new Employee
        {
            Id = newId,
            FirstName = firstName,
            LastName = lastName,
            CompanyName = companyName,
            Position = position,
            TenantId = tenantId
        };
        _employees.Add(employee);
        return employee;
    }

    private List<Employee> GenerateEmployees()
    {
        var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Emily", "Robert", "Lisa", "James", "Mary", "Santosh" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        var companies = new[] { "TechCorp", "InnovateLabs", "DataSystems", "CloudWorks", "SoftSolutions", "DigitalHub", "CodeFactory", "NetServices", "InfoTech", "WebDynamics" };
        var positions = new[] { "Software Engineer", "Senior Developer", "Project Manager", "Team Lead", "Architect", "QA Engineer", "DevOps Engineer", "Product Manager", "Scrum Master", "Business Analyst" };

        var employees = new List<Employee>();
        var random = new Random(42);

        // Generate 150 employees for tenant-customer1
        for (int i = 1; i <= 150; i++)
        {
            employees.Add(new Employee
            {
                Id = i,
                FirstName = firstNames[random.Next(firstNames.Length)],
                LastName = lastNames[random.Next(lastNames.Length)],
                CompanyName = companies[random.Next(companies.Length)],
                Position = positions[random.Next(positions.Length)],
                TenantId = "tenant-customer1"
            });
        }

        // Generate 150 employees for tenant-customer2 (using same seed for some overlap in data patterns)
        var random2 = new Random(42);
        for (int i = 151; i <= 300; i++)
        {
            employees.Add(new Employee
            {
                Id = i,
                FirstName = firstNames[random2.Next(firstNames.Length)],
                LastName = lastNames[random2.Next(lastNames.Length)],
                CompanyName = companies[random2.Next(companies.Length)],
                Position = positions[random2.Next(positions.Length)],
                TenantId = "tenant-customer2"
            });
        }

        return employees;
    }
}
