using HomeNetMonitor.Api.Features.Network;
using HomeNetMonitor.Api.Infrastructure;
using HomeNetMonitor.Api.Data;
using Microsoft.EntityFrameworkCore;
using HomeNetMonitor.Api.Features.Network.Persistence;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<NetworkInterfaceService>();
builder.Services.AddSingleton<SubnetCalculator>();
builder.Services.AddSingleton<DeviceDiscoveryService>();
builder.Services.AddSingleton<ArpTableService>();
builder.Services.AddSingleton<HostnameResolverService>();
builder.Services.AddSingleton<BonjourDiscoveryService>();
builder.Services.AddSingleton<MacVendorLookupService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<IDevicePersistenceService,DevicePersistenceService>();


var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection is not configured.");

var sqliteConnectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString)
{
    DataSource = Path.Combine(
        builder.Environment.ContentRootPath,
        "homenetmonitor.db")
}.ToString();

builder.Services.AddDbContext<HomeNetMonitorDbContext>(options =>
    options.UseSqlite(sqliteConnectionString));


builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
