using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoryMixer.Api.Entities;

namespace StoryMixer.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(user => user.Name).HasMaxLength(100);

        builder.Property(user => user.Email).HasColumnType("citext");
        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.CreatedAt).HasDefaultValueSql("now()");
    }
}
