using Microsoft.EntityFrameworkCore;
using System;

namespace VDR5
{
    public class File
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        //public long Size { get; set; }
        //public string ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class FileDbContext : DbContext
    {
        public string DbPath { get; }        
        public DbSet<File> Files { get; set; }

        public FileDbContext()
        {            
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "vdr5.db");
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<File>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Path).IsRequired();
                //entity.Property(e => e.ContentType).IsRequired();
            });
        }
    }
}