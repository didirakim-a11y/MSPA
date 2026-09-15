using MSPA.Agent;

namespace MSPA.Tests;

public class PrinterMappingServiceTests
{
    private readonly PrinterMappingService _service = new();

    [Fact]
    public void GetPrinterAssignments_Uses_Default_Printer_When_User_Has_No_Group_Filter()
    {
        var assignments = _service.GetPrinterAssignments([]);

        Assert.Contains(assignments, x => x.Share == "Reception" && x.IsDefault);
    }

    [Fact]
    public void GetPrinterAssignments_Respects_Group_Membership()
    {
        var printers = new[]
        {
            new PrinterDefinition("Reception", string.Empty, true),
            new PrinterDefinition("Accounting", "GG-Accounting"),
            new PrinterDefinition("Warehouse", "GG-Warehouse")
        };

        var assignments = _service.GetPrinterAssignments(["GG-Accounting"], printers);

        Assert.Contains(assignments, x => x.Share == "Reception");
        Assert.Contains(assignments, x => x.Share == "Accounting");
        Assert.DoesNotContain(assignments, x => x.Share == "Warehouse");
    }

    [Fact]
    public void ResolveAssignments_Returns_Default_Printer_Connection()
    {
        var result = _service.ResolveAssignments(["GG-Accounting"]);

        Assert.Equal("\\\\printserver.contoso.com\\Reception", result.DefaultPrinter);
    }
}