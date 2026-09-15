
using MSPA.Agent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<PrinterMappingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    product = "MSPA",
    features = new[] { "printer-mapping", "group-based-access", "cloud-print-integration" }
}));

app.MapGet("/api/printers", (PrinterMappingService service) =>
{
    return Results.Ok(service.DefaultPrinters.Select(printer => new
    {
        printer.Share,
        printer.Group,
        printer.IsDefault,
        connectionName = $"\\\\{printer.Server}\\{printer.Share}"
    }));
});

app.MapPost("/api/printers/map", (PrinterAssignmentRequest request, PrinterMappingService service) =>
{
    var userGroups = request.UserGroups ?? [];
    var printers = request.Printers ?? service.DefaultPrinters;
    var assignments = service.GetPrinterAssignments(userGroups, printers);
    var defaultPrinter = assignments.FirstOrDefault(x => x.IsDefault)?.ConnectionName;

    return Results.Ok(new
    {
        defaultPrinter,
        assignments
    });
});

app.Run();
