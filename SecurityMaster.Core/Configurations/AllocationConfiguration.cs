using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("Allocation");

        builder.HasKey(a => a.AllocationId);

        builder.Property(a => a.Quantity)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(a => a.Manager)
            .WithMany()
            .HasForeignKey(a => a.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Strategy)
            .WithMany()
            .HasForeignKey(a => a.StrategyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}