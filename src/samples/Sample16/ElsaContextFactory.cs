using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sample16
{
    public class ElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ElsaContext>();

            optionsBuilder.UseSqlServer(
                @"Server=127.0.0.1;Database=Elsa;User=sa;Password=sa;",
                x => x.MigrationsAssembly(typeof(Program).Assembly.FullName));

            return new ElsaContext(optionsBuilder.Options);
        }
    }
}