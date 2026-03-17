using Microsoft.EntityFrameworkCore;
using StemOMatiek.Data.Models;

namespace StemOMatiek.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Partij> Partijen => Set<Partij>();
    public DbSet<Document> Documenten => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<Beslissing> Beslissingen => Set<Beslissing>();
    public DbSet<BeslissingPartij> BeslissingPartijen => Set<BeslissingPartij>();
    public DbSet<Analyse> Analyses => Set<Analyse>();
    public DbSet<Resultaat> Resultaten => Set<Resultaat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Partij>().HasData(
            new Partij { Id = 1, Naam = "Nieuw-Vlaamse Alliantie", Afkorting = "N-VA", Kleur = "#FFB800", Zetels = 31, InRegering = true },
            new Partij { Id = 2, Naam = "Vooruit", Afkorting = "Vooruit", Kleur = "#FF2900", Zetels = 18, InRegering = true },
            new Partij { Id = 3, Naam = "Christen-Democratisch en Vlaams", Afkorting = "CD&V", Kleur = "#FF6600", Zetels = 16, InRegering = true },
            new Partij { Id = 4, Naam = "Vlaams Belang", Afkorting = "VB", Kleur = "#004A7F", Zetels = 31, InRegering = false },
            new Partij { Id = 5, Naam = "Partij van de Arbeid van België", Afkorting = "PVDA", Kleur = "#CC0000", Zetels = 9, InRegering = false },
            new Partij { Id = 6, Naam = "Open Vlaamse Liberalen en Democraten", Afkorting = "Open Vld", Kleur = "#0087DC", Zetels = 9, InRegering = false },
            new Partij { Id = 7, Naam = "Groen", Afkorting = "Groen", Kleur = "#83B81A", Zetels = 9, InRegering = false },
            new Partij { Id = 8, Naam = "Team Fouad Ahidar", Afkorting = "TFA", Kleur = "#808080", Zetels = 1, InRegering = false }
        );

        modelBuilder.Entity<BeslissingPartij>()
            .HasOne(bp => bp.Beslissing)
            .WithMany(b => b.BetrokkenPartijen)
            .HasForeignKey(bp => bp.BeslissingId);

        modelBuilder.Entity<BeslissingPartij>()
            .HasOne(bp => bp.Partij)
            .WithMany(p => p.BeslissingPartijen)
            .HasForeignKey(bp => bp.PartijId);

        modelBuilder.Entity<Analyse>()
            .HasOne(a => a.Beslissing)
            .WithMany(b => b.Analyses)
            .HasForeignKey(a => a.BeslissingId);

        modelBuilder.Entity<Analyse>()
            .HasOne(a => a.Partij)
            .WithMany(p => p.Analyses)
            .HasForeignKey(a => a.PartijId);

        modelBuilder.Entity<Resultaat>()
            .HasOne(r => r.Beslissing)
            .WithOne(b => b.Resultaat)
            .HasForeignKey<Resultaat>(r => r.BeslissingId);
    }
}
