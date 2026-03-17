namespace StemOMatiek.Data.Models;

public class Partij
{
    public int Id { get; set; }
    public string Naam { get; set; } = string.Empty;
    public string Afkorting { get; set; } = string.Empty;
    public string Kleur { get; set; } = "#888888";
    public int Zetels { get; set; }
    public bool InRegering { get; set; }
    public string? LogoUrl { get; set; }

    public ICollection<Document> Documenten { get; set; } = new List<Document>();
    public ICollection<BeslissingPartij> BeslissingPartijen { get; set; } = new List<BeslissingPartij>();
    public ICollection<Analyse> Analyses { get; set; } = new List<Analyse>();
}
