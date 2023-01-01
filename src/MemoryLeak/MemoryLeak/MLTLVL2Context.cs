using Microsoft.EntityFrameworkCore;
// using MemoryLeak.DataModels.MeltLevel2;

namespace MemoryLeak.DataModels
{
    public partial class MLTLVL2Context : DbContext
    {

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //var connectionString = "Server=localhost;Port=1521;User Id=foo;Password=abcd1234;Database=ORCLPDB1;";
            var connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ORCLPDB1)));User Id=foo;Password=abcd1234;";
            optionsBuilder.UseOracle(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //For when tables don't have primary Keys
            builder.Entity<EAF_BKT_YARD>().HasKey(t => new { t.C_STN_ID, t.C_YARD_ID, t.C_PILE_NUM });
            builder.Entity<EAF_BKT_ASSGN_MAT>().HasKey(t => new { t.C_Toast });
        }

        #region Entities

        public DbSet<EAF_BKT_ASSGN_MAT>? EAF_BKT_ASSGN_MAT { get; set; }
        public DbSet<EAF_BKT_YARD>? EAF_BKT_YARD { get; set; }

        #endregion
    }
}
