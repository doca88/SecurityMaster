using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Order");

        builder.HasKey(o => o.OrderId);

        builder.Property(o => o.Quantity)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.TradeDate)
            .IsRequired();

        // FK -> Manager (bez povratne kolekcije na Manager strani)
        builder.HasOne(o => o.Manager)
            .WithMany()
            .HasForeignKey(o => o.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK -> Strategy (bez povratne kolekcije na Strategy strani)
        builder.HasOne(o => o.Strategy)
            .WithMany()
            .HasForeignKey(o => o.StrategyId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK -> Security (Security ne zna za Order)
        builder.HasOne(o => o.Security)
            .WithMany()
            .HasForeignKey(o => o.SID)
            .OnDelete(DeleteBehavior.Restrict);

        // Order -> Allocation (1:N, Order zna za svoje alokacije)
        builder.HasMany(o => o.Allocations)
            .WithOne(a => a.Order)
            .HasForeignKey(a => a.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}