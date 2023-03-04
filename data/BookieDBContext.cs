using Bakalauras.Auth.Model;
using Bakalauras.data.entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Configuration;

namespace Bakalauras.data
{
    public class BookieDBContext : IdentityDbContext<BookieUser>
    {
        //private readonly IConfiguration _configuration;

        //public BookieDBContext(IConfiguration configuration, DbContextOptions<BookieDBContext> options)
        //    : base(options)
        //{
        //    _configuration = configuration;
        //}
        public DbSet<Book> Books { get; set; }
        public DbSet<Comment> Comments { get; set; }

        public DbSet<Answer> Answers { get; set; }

        public DbSet<Genre> Genres { get; set; }

        public DbSet<Chapter> Chapters { get; set; }

        public DbSet<DailyQuestion> DailyQuestions { get; set; }

        public DbSet<DailyQuestionProfile> DailyQuestionProfiles { get; set; }

        public DbSet<Profile> Profiles { get; set; }

        public DbSet<ProfileBook> ProfileBooks { get; set; }

        public DbSet<ProfileText> ProfileTexts { get; set; }

        public DbSet<Text> Texts { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=BookieDB");
            //  optionsBuilder.UseSqlServer("Server=tcp:bookie.database.windows.net,1433;Initial Catalog=Bookie_db;Persist Security Info=False;User ID=namas;Password=AdminBasket18+;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
        }
    }

}
