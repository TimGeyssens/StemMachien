namespace StemOMatiek.Data.Models;

public class Beslissing
{
    public int Id { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string Beschrijving { get; set; } = string.Empty;
    public DateTime Datum { get; set; } = DateTime.UtcNow;
    public string? Bron { get; set; }
    public string Status { get; set; } = "Voorgesteld"; // Voorgesteld, Goedgekeurd, Uitgevoerd, Verworpen

    public ICollection<BeslissingPartij> BetrokkenPartijen { get; set; } = new List<BeslissingPartij>();
    public ICollection<Analyse> Analyses { get; set; } = new List<Analyse>();
    public Resultaat? Resultaat { get; set; }
}

public class BeslissingPartij
{
    public int Id { get; set; }
    public int BeslissingId { get; set; }
    public int PartijId { get; set; }
    public string Rol { get; set; } = "Voorsteller"; // Voorsteller, Tegenstander, Onthoudend

    public Beslissing Beslissing { get; set; } = null!;
    public Partij Partij { get; set; } = null!;
}
