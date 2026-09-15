namespace MSPA.Agent;

public sealed record PrinterDefinition(
    string Share,
    string? Group = null,
    bool IsDefault = false,
    string Server = "printserver.contoso.com");

public sealed record PrinterAssignment(
    string Share,
    string ConnectionName,
    bool IsDefault,
    bool Mapped,
    string? Group = null);

public sealed record PrinterAssignmentRequest(
    IReadOnlyList<string>? UserGroups = null,
    IReadOnlyList<PrinterDefinition>? Printers = null);

public sealed record PrinterAssignmentResult(
    IReadOnlyList<PrinterAssignment> Assignments,
    string? DefaultPrinter)
{
    public bool HasDefault => !string.IsNullOrWhiteSpace(DefaultPrinter);
}

public sealed class PrinterMappingService
{
    public IReadOnlyList<PrinterDefinition> DefaultPrinters { get; } =
    [
        new("Reception", Group: string.Empty, IsDefault: true),
        new("Accounting", Group: "GG-Accounting"),
        new("Warehouse", Group: "GG-Warehouse"),
        new("HR-Secure", Group: "GG-HR")
    ];

    public IReadOnlyList<PrinterAssignment> GetPrinterAssignments(
        IEnumerable<string>? userGroups,
        IEnumerable<PrinterDefinition>? printers = null)
    {
        var items = (printers ?? DefaultPrinters).ToList();
        var groups = NormalizeUserGroups(userGroups);

        var assignments = new List<PrinterAssignment>();
        foreach (var printer in items)
        {
            var isEligible = IsAllowedForGroups(printer.Group, groups);
            if (!isEligible)
            {
                continue;
            }

            var connectionName = $"\\\\{printer.Server}\\{printer.Share}";
            assignments.Add(new PrinterAssignment(
                Share: printer.Share,
                ConnectionName: connectionName,
                IsDefault: printer.IsDefault,
                Mapped: true,
                Group: printer.Group));
        }

        return assignments;
    }

    public IReadOnlyList<PrinterAssignment> MapPrinters(
        IEnumerable<string>? userGroups,
        IEnumerable<PrinterDefinition>? printers = null)
    {
        return GetPrinterAssignments(userGroups, printers);
    }

    public PrinterAssignmentResult ResolveAssignments(
        IEnumerable<string>? userGroups,
        IEnumerable<PrinterDefinition>? printers = null)
    {
        var assignments = GetPrinterAssignments(userGroups, printers);
        var defaultPrinter = assignments
            .Where(x => x.IsDefault)
            .Select(x => x.ConnectionName)
            .FirstOrDefault();

        return new PrinterAssignmentResult(assignments, defaultPrinter);
    }

    private static HashSet<string> NormalizeUserGroups(IEnumerable<string>? userGroups)
    {
        var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (userGroups is null)
        {
            return groups;
        }

        foreach (var group in userGroups)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                continue;
            }

            groups.Add(group.Trim());
            if (group.Contains('\\'))
            {
                groups.Add(group[(group.LastIndexOf('\\') + 1)..]);
            }
        }

        return groups;
    }

    private static bool IsAllowedForGroups(string? groupName, HashSet<string> userGroups)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return true;
        }

        var candidate = groupName.Trim();
        return userGroups.Contains(candidate)
            || userGroups.Contains(candidate[(candidate.LastIndexOf('\\') + 1)..])
            || userGroups.Contains(candidate[(candidate.IndexOf('\\') + 1)..]);
    }
}
