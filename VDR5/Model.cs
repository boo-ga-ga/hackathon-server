using Microsoft.EntityFrameworkCore;
using System;

namespace VDR5
{
    public class File
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string InternalName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        
        public DateTime UploadedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FileDbContext : DbContext
    {
        public string DbPath { get; }        
        public DbSet<File> Files { get; set; }
        
        public FileDbContext(DbContextOptions<FileDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<File>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.FullPath).IsRequired();

                entity.HasIndex(e => e.FullPath).IsUnique();
                
            });
        }
    }
}