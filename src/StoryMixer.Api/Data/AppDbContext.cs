using Microsoft.EntityFrameworkCore;

namespace StoryMixer.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
