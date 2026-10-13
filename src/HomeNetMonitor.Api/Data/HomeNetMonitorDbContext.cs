using HomeNetMonitor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeNetMonitor.Api.Data;

public class HomeNetMonitorDbContext : DbContext
{
    public HomeNetMonitorDbContext(DbContextOptions<HomeNetMonitorDbContext> options) : base(options)
    {
    }

    public DbSet<NetworkDevice> NetworkDevices {get; set;}
    public DbSet<DeviceObservations> DeviceObservation {get; set;}


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeviceObservations>()
            .HasOne(observation => observation.NetworkDevice)
            .WithMany(device => device.Observations)
            .HasForeignKey(observation => observation.NetworkDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeviceObservations>()
                .HasIndex(observation => new
                {
                    observation.NetworkDeviceId,
                    observation.ObservedAtUtc
                });       
    }
}