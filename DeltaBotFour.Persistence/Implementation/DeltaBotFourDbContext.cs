using Microsoft.EntityFrameworkCore;
using System;
using DeltaBotFour.Models;

namespace DeltaBotFour.Persistence.Implementation
{
    internal class DeltaBotFourDbContext : DbContext
    {
        public DbSet<DeltaComment> DeltaComments { get; set; }
        public DbSet<Deltaboard> Deltaboards { get; set; }
        public DbSet<DeltaboardEntry> DeltaboardEntries { get; set; }
        public DbSet<DeltaLogPostMapping> DeltaLogPostMappings { get; set; }
        public DbSet<DB4State> Db4States { get; set; }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source=DeltaBotFourSqlLite.db");
    }
}