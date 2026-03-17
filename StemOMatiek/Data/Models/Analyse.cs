namespace StemOMatiek.Data.Models;

public class Analyse
{
    public int Id { get; set; }
    public int BeslissingId { get; set; }
    public int PartijId { get; set; }
    public DateTime DatumAnalyse { get; set; } = DateTime.UtcNow;

    /// <summary>Score van 0-100 die aangeeft hoe goed de beslissing overeenkomt met het partijprogramma</summary>
    public int OvereenkomstScore { get; set; }

    /// <summary>AI-gegenereerd sarcastisch commentaar in oud-Vlaams</summary>
    public string Commentaar { get; set; } = string.Empty;

    /// <summary>Samenvatting van de analyse</summary>
    public string Samenvatting { get; set; } = string.Empty;

    /// <summary>Comma-separated IDs van relevante DocumentChunks</summary>
    public string? RelevanteChunkIds { get; set; }

    public Beslissing Beslissing { get; set; } = null!;
    public Partij Partij { get; set; } = null!;
}
