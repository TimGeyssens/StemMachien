# ✦ Stem-O-Matiek — Het Analytisch Stem-Machien ✦

> *„Een politieker die zijn eigen programma leest — dat is pas revolutionair!"*

## Wat is dit machien?

**Stem-O-Matiek** is een satirisch webbouwsel dat met behulp van Kunstmatige Intelligentie de beslissingen der Vlaamsche regeering toetst aen de partijprogramma's ende verkiezingsbeloften der acht Vlaamsche partijen.

Gij kunt het machien raedplegen als **Wakkere Burger** (toets alle partijen aen hunne woorden ende daden) of als **Politieker** (ontdek wat uwe eigen partij ooit beloofde — ende wat ervan terechtkwam).

## De Gilden des Parlements

Het machien kent de volgende acht partijen:

- **N-VA** — Nieuw-Vlaamse Alliantie
- **Vlaams Belang**
- **Vooruit**
- **CD&V** — Christen-Democratisch en Vlaams
- **Open Vld** — Open Vlaamse Liberalen en Democraten
- **PVDA** — Partij van de Arbeid
- **Groen**
- **Team Fouad Ahidar**

## Hoe werkt het machien?

1. **Perkamenten inladen** — Voeg partijprogramma's toe via *De Gildekamers*. Het machien indexeert deze met embeddings.
2. **Decreten optekenen** — Voeg regeringsbeslissingen toe via *Het Decretenlogboeck*.
3. **De Waerheydsmachien** — Laet de AI elke beslissing vergelijken met de partijprogramma's en een overeenkomstscore berekenen.
4. **Den Stellingentoets** — Beantwoord stellingen ende ontdek welke partij het dichtst bij uwe meening staet.

## Technologie

- **.NET 10** met **Blazor Server** (interactieve server-side rendering)
- **MudBlazor 9** voor de retro-futuristische interface
- **Entity Framework Core** met **SQLite** als databank
- **Microsoft Semantic Kernel** met **Google Gemini** (`gemini-3.1-flash-lite-preview` voor chat, `gemini-embedding-001` voor embeddings)

## Aen de slag

### Vereisten

- .NET 10 SDK
- Een Google Gemini API-sleutel

### Opstarten

```bash
cd StemOMatiek
dotnet run
```

Navigeer naer `http://localhost:5092` ende voer uwen API-sleutel in via *De Sleutelmeester*.

## Waerschuwing

Dit is een satirisch hulpmiddel, gebouwd met eene kwinkslag. De analyses worden gegenereerd door AI ende zijn geenszins juridisch of politiek bindend. Gebruik uw eigen verstand — indien voorhanden.

---

*Gebouwd met ✦ ende eene gezonde dosis Vlaamsche zelspot.*
