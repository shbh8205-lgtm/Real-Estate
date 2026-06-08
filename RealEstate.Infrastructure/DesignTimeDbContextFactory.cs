using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RealEstate.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        // שים לב: ודא שה-ConnectionString כאן זהה לזה שב-appsettings.json שלך
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RealEstateDb;Trusted_Connection=True;");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}