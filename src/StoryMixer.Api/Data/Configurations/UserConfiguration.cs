using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoryMixer.Api.Entities;

namespace StoryMixer.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Name).HasMaxLength(100);

        builder.Property(u => u.Email).HasColumnType("citext");
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
    }
}
