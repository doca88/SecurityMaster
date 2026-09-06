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

        builder.HasOne(o => o.Manager)
            .WithMany()
            .HasForeignKey(o => o.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Strategy)
            .WithMany()
            .HasForeignKey(o => o.StrategyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Security)
            .WithMany()
            .HasForeignKey(o => o.Sid)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Allocations)
            .WithOne(a => a.Order)
            .HasForeignKey(a => a.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}