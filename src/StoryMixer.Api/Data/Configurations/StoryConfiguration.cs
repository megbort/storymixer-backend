using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoryMixer.Api.Entities;

namespace StoryMixer.Api.Data.Configurations;

public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        builder.HasOne(story => story.User)
            .WithMany(user => user.Stories)
            .HasForeignKey(story => story.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(story => story.CreatedAt).HasDefaultValueSql("now()");
    }
}
