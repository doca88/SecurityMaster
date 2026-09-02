using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class StrategyConfiguration : IEntityTypeConfiguration<Strategy>
{
    public void Configure(EntityTypeBuilder<Strategy> builder)
    {
        builder.ToTable("Strategy");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Display)
            .IsRequired()
            .HasMaxLength(100);
    }
}