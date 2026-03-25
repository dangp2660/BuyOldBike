using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuyOldBike_DAL.Entities;

public partial class BuyOldBikeContext
{
    private string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        var strConn = config["ConnectionStrings:DefaultConnection"];
        if (string.IsNullOrWhiteSpace(strConn))
            throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection trong appsettings.json.");

        return strConn;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(GetConnectionString());
    }
}
