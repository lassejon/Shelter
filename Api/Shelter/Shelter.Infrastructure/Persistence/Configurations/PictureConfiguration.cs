using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shelter.Domain.Pictures;

namespace Shelter.Infrastructure.Persistence.Configurations;

public class PictureConfiguration : IEntityTypeConfiguration<Picture>
{
    public void Configure(EntityTypeBuilder<Picture> builder)
    {
        builder.ToTable("Pictures", t =>
            t.HasCheckConstraint(
                "CK_Pictures_TargetScope",
                "(\"Scope\" = 0 AND \"ShelterId\" IS NOT NULL AND \"ReviewId\" IS NULL) OR " +
                "(\"Scope\" = 1 AND \"ReviewId\" IS NOT NULL AND \"ShelterId\" IS NULL)"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Scope)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.Caption)
            .HasMaxLength(400);

        builder.Property(x => x.SortOrder)
            .HasDefaultValue(0);

        builder.HasOne(x => x.Owner)
            .WithMany(u => u.Pictures)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Shelter)
            .WithMany(s => s.Pictures)
            .HasForeignKey(x => x.ShelterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Review)
            .WithMany(r => r.Pictures)
            .HasForeignKey(x => x.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
