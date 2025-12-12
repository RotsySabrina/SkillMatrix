using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.Models;

namespace SkillMatrix.Data.EF
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Consultant> Consultants => Set<Consultant>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<ConsultantSkill> ConsultantSkills => Set<ConsultantSkill>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ConsultantSkill>()
                .HasKey(cs => new { cs.ConsultantId, cs.SkillId });

            modelBuilder.Entity<ConsultantSkill>()
                .HasOne(cs => cs.Consultant)
                .WithMany(c => c.ConsultantSkills)
                .HasForeignKey(cs => cs.ConsultantId);

            modelBuilder.Entity<ConsultantSkill>()
                .HasOne(cs => cs.Skill)
                .WithMany(s => s.ConsultantSkills)
                .HasForeignKey(cs => cs.SkillId);
        }
    }
}
