using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ManagerConfiguration : IEntityTypeConfiguration<Manager>
{
    public void Configure(EntityTypeBuilder<Manager> builder)
    {
        builder.ToTable("Manager");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Display)
            .IsRequired()
            .HasMaxLength(100);
    }
}