using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
public class SecurityConfiguration : IEntityTypeConfiguration<Security>
{
    public void Configure(EntityTypeBuilder<Security> builder)
    {
        builder.ToTable("Security");

        builder.HasKey(s => s.Sid);

        builder.Property(s => s.Sid)
            .HasColumnName("SID")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.Description)
            .IsRequired()
            .HasMaxLength(255);
    }
}