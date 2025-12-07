using Microsoft.EntityFrameworkCore;
using PddTrainer.Api.Models;

namespace PddTrainer.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
        public DbSet<Theme> Themes => Set<Theme>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ticket → Questions
            modelBuilder.Entity<Ticket>()
                .HasMany(t => t.Questions)
                .WithOne(q => q.Ticket)
                .HasForeignKey(q => q.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question → AnswerOptions
            modelBuilder.Entity<Question>()
                .HasMany(q => q.AnswerOptions)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question → Theme
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Theme)
                .WithMany(t => t.Questions)
                .HasForeignKey(q => q.ThemeId)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}
