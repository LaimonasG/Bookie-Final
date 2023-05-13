using Bakalauras.Auth.Model;
using Bakalauras.data.entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bakalauras.data
{
    public class BookieDBContext : IdentityDbContext<BookieUser>
    {
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

        public DbSet<Payment> Payments { get; set; }

        public DbSet<PaymentUser> PaymentUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<DailyQuestionProfile>()
                .HasKey(x => new { x.DailyQuestionId, x.ProfileId });

            modelBuilder.Entity<DailyQuestionProfile>()
                .HasOne(p => p.DailyQuestion)
                .WithMany(pu => pu.DailyQuestionProfiles)
                .HasForeignKey(p => p.DailyQuestionId);

            modelBuilder.Entity<DailyQuestionProfile>()
                .HasOne(u => u.Profile)
                .WithMany(pu => pu.DailyQuestionProfiles)
                .HasForeignKey(u => u.ProfileId);

            modelBuilder.Entity<PaymentUser>()
               .HasKey(x => new { x.Id });

            modelBuilder.Entity<PaymentUser>()
                .HasOne(p => p.Profile)
                .WithMany(pu => pu.PaymentUser)
                .HasForeignKey(p => p.ProfileId);

            modelBuilder.Entity<PaymentUser>()
                .HasOne(u => u.Payment)
                .WithMany(pu => pu.PaymentUser)
                .HasForeignKey(u => u.PaymentId);

            modelBuilder.Entity<ProfileBook>()
                .HasKey(x => new { x.BookId, x.ProfileId });

            modelBuilder.Entity<ProfileBook>()
                .HasOne(p => p.Profile)
                .WithMany(pu => pu.ProfileBooks)
                .HasForeignKey(p => p.ProfileId);

            modelBuilder.Entity<ProfileText>()
                .HasKey(x => new { x.TextId, x.ProfileId });

            modelBuilder.Entity<ProfileText>()
                .HasOne(u => u.Profile)
                .WithMany(pu => pu.ProfileTexts)
                .HasForeignKey(u => u.ProfileId);

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=BookieDB");
            }
        }
    }

}
