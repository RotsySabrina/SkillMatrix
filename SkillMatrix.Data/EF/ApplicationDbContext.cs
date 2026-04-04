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
        public DbSet<User> Users { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Mission> Missions { get; set; }
        public DbSet<MissionSkill> MissionSkills { get; set; }

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

            modelBuilder.Entity<MissionSkill>()
                .HasKey(ms => new { ms.MissionId, ms.SkillId });

            modelBuilder.Entity<Mission>()
                .HasOne(m => m.Consultant)
                .WithMany(c => c.Missions)
                .HasForeignKey(m => m.ConsultantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Mission>()
                .HasOne(m => m.Client)
                .WithMany(c => c.Missions)
                .HasForeignKey(m => m.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MissionSkill>()
                .HasOne(ms => ms.Mission)
                .WithMany(m => m.MissionSkills)
                .HasForeignKey(ms => ms.MissionId);

            modelBuilder.Entity<MissionSkill>()
                .HasOne(ms => ms.Skill)
                .WithMany()
                .HasForeignKey(ms => ms.SkillId);
        }
    }
}