using Microsoft.EntityFrameworkCore;

class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
}
