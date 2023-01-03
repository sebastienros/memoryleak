using Microsoft.EntityFrameworkCore;
// using MemoryLeak.DataModels.MeltLevel2;

namespace MemoryLeak.DataModels
{
    public partial class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
            this.Database.EnsureCreated();
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //var connectionString = "Server=localhost;Port=1521;User Id=foo;Password=abcd1234;Database=ORCLPDB1;";
            var connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ORCLPDB1)));User Id=foo;Password=abcd1234;";
            optionsBuilder.UseOracle(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //For when tables don't have primary Keys
            builder.Entity<T_TOAST>().HasKey(t => new { t.C_ID });
        }

        #region Entities

        public DbSet<T_TOAST>? T_TOAST { get; set; }

        #endregion
    }
}
