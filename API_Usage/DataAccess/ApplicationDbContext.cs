using Microsoft.EntityFrameworkCore;
using API_Usage.Models;

namespace API_Usage.DataAccess
{
  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Equity> Equities { get; set; }
    public DbSet<API_Usage.Models.GainersList> GainersList { get; set; }
    public DbSet<Sector> Sector { get; set; }
    public DbSet<FinancialList> FinancialList { get; set; }
        public DbSet<outputModel> stockSuggest { get; set; }
    }
}