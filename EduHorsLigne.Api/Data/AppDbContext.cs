using EduHorsLigne.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduHorsLigne.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Classe> Classes => Set<Classe>();
    public DbSet<Matiere> Matieres => Set<Matiere>();
    public DbSet<Chapitre> Chapitres => Set<Chapitre>();
    public DbSet<Lecon> Lecons => Set<Lecon>();
    public DbSet<LeconPackage> Packages => Set<LeconPackage>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<MarquePage> MarquePages => Set<MarquePage>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Favori> Favoris => Set<Favori>();
    public DbSet<ProgressionLecon> Progressions => Set<ProgressionLecon>();
    public DbSet<Signalement> Signalements => Set<Signalement>();
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Identifiant).IsUnique();

        // Hiérarchie du catalogue (RG1) : suppression refusée tant qu'il reste des enfants (contrôlée dans l'API)
        b.Entity<Classe>().HasMany(c => c.Matieres).WithOne(m => m.Classe).HasForeignKey(m => m.ClasseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Matiere>().HasMany(m => m.Chapitres).WithOne(c => c.Matiere).HasForeignKey(c => c.MatiereId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Chapitre>().HasMany(c => c.Lecons).WithOne(l => l.Chapitre).HasForeignKey(l => l.ChapitreId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Lecon>().HasMany<LeconPackage>().WithOne().HasForeignKey(p => p.LeconId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Lecon>().HasOne<Quiz>().WithOne().HasForeignKey<Quiz>(q => q.LeconId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<LeconPackage>().HasIndex(p => new { p.LeconId, p.Version }).IsUnique();

        // Données de l'élève : supprimées avec le compte (EF-88). Pas de FK vers Lecon (RG3 : elles survivent à la leçon).
        b.Entity<MarquePage>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Note>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Favori>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProgressionLecon>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Evaluation>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Favori>().HasIndex(x => new { x.UserId, x.LeconId }).IsUnique();
        b.Entity<ProgressionLecon>().HasIndex(x => new { x.UserId, x.LeconId }).IsUnique();
        b.Entity<Evaluation>().HasIndex(x => new { x.UserId, x.LeconId }).IsUnique();
        b.Entity<MarquePage>().HasIndex(x => new { x.UserId, x.ServerModifiedUtc });
        b.Entity<Note>().HasIndex(x => new { x.UserId, x.ServerModifiedUtc });
    }
}
