using CF_Cache_API.Models;

namespace CF_Cache_API.Services;

public class EmployeeService
{
    private readonly List<Employee> _employees;

    public EmployeeService()
    {
        _employees = GenerateEmployees();
    }

    public IEnumerable<Employee> GetAll(string tenantId, int page = 1, int pageSize = 10, List<string>? entities = null)
    {
        return ApplyEntityFilter(_employees.Where(e => e.TenantId == tenantId), entities)
            .Skip((page - 1) * pageSize).Take(pageSize);
    }

    public (IEnumerable<Employee>, int) SearchByFirstName(string tenantId, string firstName, int page = 1, int pageSize = 10, List<string>? entities = null)
    {
        var query = ApplyEntityFilter(_employees.Where(e => e.TenantId == tenantId && e.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase)), entities);
        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public (IEnumerable<Employee>, int) SearchByLastName(string tenantId, string lastName, int page = 1, int pageSize = 10, List<string>? entities = null)
    {
        var query = ApplyEntityFilter(_employees.Where(e => e.TenantId == tenantId && e.LastName.Contains(lastName, StringComparison.OrdinalIgnoreCase)), entities);
        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public (IEnumerable<Employee>, int) SearchByCompany(string tenantId, string companyName, int page = 1, int pageSize = 10, List<string>? entities = null)
    {
        var query = ApplyEntityFilter(_employees.Where(e => e.TenantId == tenantId && e.CompanyName.Contains(companyName, StringComparison.OrdinalIgnoreCase)), entities);
        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public (IEnumerable<Employee>, int) SearchByPosition(string tenantId, string position, int page = 1, int pageSize = 10, List<string>? entities = null)
    {
        var query = ApplyEntityFilter(_employees.Where(e => e.TenantId == tenantId && e.Position.Contains(position, StringComparison.OrdinalIgnoreCase)), entities);
        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public (IEnumerable<Employee>, int) Search(string tenantId, string? firstName, string? lastName, string? companyName, string? position, int page = 1, int pageSize = 10, List<string>? entities = null)
    {
        var query = ApplyEntityFilter(_employees.Where(e => e.TenantId == tenantId), entities);

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

    public (IEnumerable<Employee>, int) SearchByDepartment(string tenantId, string? firstName, string? department, int page = 1, int pageSize = 50, List<string>? entities = null)
    {
        var query = ApplyEntityFilter(_employees.Where(e => e.TenantId == tenantId), entities);

        if (!string.IsNullOrEmpty(firstName))
            query = query.Where(e => e.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(department))
            query = query.Where(e => e.Department.Contains(department, StringComparison.OrdinalIgnoreCase));

        return (query.Skip((page - 1) * pageSize).Take(pageSize), query.Count());
    }

    public int GetTotalCount(string tenantId, List<string>? entities = null) =>
        ApplyEntityFilter(_employees.Where(e => e.TenantId == tenantId), entities).Count();

    public Employee AddEmployee(string tenantId, string firstName, string lastName, string companyName, string position, string? entity = null)
    {
        var newId = _employees.Any() ? _employees.Max(e => e.Id) + 1 : 1;
        var employee = new Employee
        {
            Id = newId,
            FirstName = firstName,
            LastName = lastName,
            CompanyName = companyName,
            Position = position,
            Entity = entity ?? "",
            TenantId = tenantId
        };
        _employees.Add(employee);
        return employee;
    }

    public Employee? UpdateEmployee(string tenantId, int id, string firstName, string lastName, string companyName, string position, string department)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id && e.TenantId == tenantId);
        if (employee == null) return null;

        employee.FirstName = firstName;
        employee.LastName = lastName;
        employee.CompanyName = companyName;
        employee.Position = position;
        employee.Department = department;
        return employee;
    }

    public bool DeleteEmployee(string tenantId, int id)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id && e.TenantId == tenantId);
        if (employee == null) return false;
        return _employees.Remove(employee);
    }

    private static IEnumerable<Employee> ApplyEntityFilter(IEnumerable<Employee> query, List<string>? entities)
    {
        if (entities != null && entities.Count > 0)
            query = query.Where(e => entities.Any(ent => e.Entity.Equals(ent, StringComparison.OrdinalIgnoreCase)));
        return query;
    }

    private List<Employee> GenerateEmployees()
    {
        var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Emily", "Robert", "Lisa", "James", "Mary", "Santosh" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        var companies = new[] { "TechCorp", "InnovateLabs", "DataSystems", "CloudWorks", "SoftSolutions", "DigitalHub", "CodeFactory", "NetServices", "InfoTech", "WebDynamics" };
        var positions = new[] { "Software Engineer", "Senior Developer", "Project Manager", "Team Lead", "Architect", "QA Engineer", "DevOps Engineer", "Product Manager", "Scrum Master", "Business Analyst" };
        var departments = new[] { "Engineering", "Sales", "Marketing", "HR", "Finance", "Operations", "IT", "Support", "Research", "Legal" };

        var employees = new List<Employee>();
        var random = new Random(42);

        // Generate 300 employees for tenant-customer1
        for (int i = 1; i <= 300; i++)
        {
            employees.Add(new Employee
            {
                Id = i,
                FirstName = firstNames[random.Next(firstNames.Length)],
                LastName = lastNames[random.Next(lastNames.Length)],
                CompanyName = companies[random.Next(companies.Length)],
                Position = positions[random.Next(positions.Length)],
                Department = departments[random.Next(departments.Length)],
                TenantId = "tenant-customer1"
            });
        }

        // Generate 300 employees for tenant-customer2 with different seed
        var random2 = new Random(100);
        for (int i = 301; i <= 600; i++)
        {
            employees.Add(new Employee
            {
                Id = i,
                FirstName = firstNames[random2.Next(firstNames.Length)],
                LastName = lastNames[random2.Next(lastNames.Length)],
                CompanyName = companies[random2.Next(companies.Length)],
                Position = positions[random2.Next(positions.Length)],
                Department = departments[random2.Next(departments.Length)],
                TenantId = "tenant-customer2"
            });
        }

        // Generate employees for tenant-customer3 across 6 entities
        var random3 = new Random(200);
        var id = 601;

        // --- User A entities: Cigna & Apollo ---
        for (int j = 0; j < 40; j++)
        {
            employees.Add(new Employee
            {
                Id = id++, FirstName = firstNames[random3.Next(firstNames.Length)],
                LastName = lastNames[random3.Next(lastNames.Length)], CompanyName = companies[random3.Next(companies.Length)],
                Position = positions[random3.Next(positions.Length)], Department = departments[random3.Next(departments.Length)],
                Entity = "Cigna", TenantId = "tenant-customer3"
            });
        }
        for (int j = 0; j < 40; j++)
        {
            employees.Add(new Employee
            {
                Id = id++, FirstName = firstNames[random3.Next(firstNames.Length)],
                LastName = lastNames[random3.Next(lastNames.Length)], CompanyName = companies[random3.Next(companies.Length)],
                Position = positions[random3.Next(positions.Length)], Department = departments[random3.Next(departments.Length)],
                Entity = "Apollo", TenantId = "tenant-customer3"
            });
        }
        // 10 overlapping in both Cigna and Apollo
        for (int j = 0; j < 10; j++)
        {
            var fn = firstNames[random3.Next(firstNames.Length)];
            var ln = lastNames[random3.Next(lastNames.Length)];
            var comp = companies[random3.Next(companies.Length)];
            var pos = positions[random3.Next(positions.Length)];
            var dept = departments[random3.Next(departments.Length)];
            employees.Add(new Employee { Id = id++, FirstName = fn, LastName = ln, CompanyName = comp, Position = pos, Department = dept, Entity = "Cigna", TenantId = "tenant-customer3" });
            employees.Add(new Employee { Id = id++, FirstName = fn, LastName = ln, CompanyName = comp, Position = pos, Department = dept, Entity = "Apollo", TenantId = "tenant-customer3" });
        }

        // --- User B entities: HCA, Fortis, Sakra, Manipal ---
        var userBEntities = new[] { "HCA", "Fortis", "Sakra", "Manipal" };
        foreach (var entityName in userBEntities)
        {
            for (int j = 0; j < 40; j++)
            {
                employees.Add(new Employee
                {
                    Id = id++, FirstName = firstNames[random3.Next(firstNames.Length)],
                    LastName = lastNames[random3.Next(lastNames.Length)], CompanyName = companies[random3.Next(companies.Length)],
                    Position = positions[random3.Next(positions.Length)], Department = departments[random3.Next(departments.Length)],
                    Entity = entityName, TenantId = "tenant-customer3"
                });
            }
        }
        // 10 overlapping across all 4 of user B's entities
        for (int j = 0; j < 10; j++)
        {
            var fn = firstNames[random3.Next(firstNames.Length)];
            var ln = lastNames[random3.Next(lastNames.Length)];
            var comp = companies[random3.Next(companies.Length)];
            var pos = positions[random3.Next(positions.Length)];
            var dept = departments[random3.Next(departments.Length)];
            foreach (var entityName in userBEntities)
            {
                employees.Add(new Employee { Id = id++, FirstName = fn, LastName = ln, CompanyName = comp, Position = pos, Department = dept, Entity = entityName, TenantId = "tenant-customer3" });
            }
        }

        return employees;
    }
}
