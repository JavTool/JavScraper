//using Microsoft.EntityFrameworkCore;
//using JavScraper.Domain;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace JavScraper.Database
//{
//    public class JavScraperContext : DbContext
//    {
//        public DbSet<Movie> Movies { get; set; }
//        public DbSet<Actres> Actress { get; set; }
//        public DbSet<Category> Categories { get; set; }
//        public DbSet<MoviePicture> MoviePictures { get; set; }

//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            optionsBuilder.UseSqlite("Filename=./Movies.db");
//        }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {

//            //modelBuilder.Entity<Movie>()
//            //  .HasKey(m => new { m.Id, m.Title });
//        }
//    }
//}
