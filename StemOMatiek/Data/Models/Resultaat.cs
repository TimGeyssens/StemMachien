namespace StemOMatiek.Data.Models;

public class Resultaat
{
    public int Id { get; set; }
    public int BeslissingId { get; set; }
    public string Beschrijving { get; set; } = string.Empty;
    public DateTime DatumIngevoerd { get; set; } = DateTime.UtcNow;

    /// <summary>Score van 0-100: hoe goed komt het resultaat overeen met de oorspronkelijke belofte</summary>
    public int BelofteBarometerScore { get; set; }

    /// <summary>AI-gegenereerd sarcastisch commentaar over het verschil tussen belofte en realiteit</summary>
    public string? AiCommentaar { get; set; }

    public Beslissing Beslissing { get; set; } = null!;
}
