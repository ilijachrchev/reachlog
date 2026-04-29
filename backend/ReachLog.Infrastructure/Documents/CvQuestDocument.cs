using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReachLog.Infrastructure.Models;

namespace ReachLog.Infrastructure.Documents;

internal sealed class CvQuestDocument : IDocument
{
    private readonly ParsedCv _cv;

    internal CvQuestDocument(ParsedCv cv) => _cv = cv;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.MarginTop(36);
            page.MarginBottom(36);
            page.MarginLeft(50);
            page.MarginRight(50);
            page.DefaultTextStyle(t => t.FontFamily(CvFonts.Family).FontSize(10).FontColor("#000000"));

            page.Content().Column(col =>
            {
                col.Spacing(0);
                RenderHeader(col);
                foreach (var section in _cv.Sections)
                    RenderSection(col, section);
            });
        });
    }

    private void RenderHeader(ColumnDescriptor col)
    {
        col.Item()
            .Text(_cv.Name)
            .FontSize(20)
            .Bold()
            .FontFamily(CvFonts.Family);

        if (!string.IsNullOrWhiteSpace(_cv.Contact))
        {
            col.Item()
                .PaddingTop(3)
                .Text(_cv.Contact)
                .FontSize(9.5f)
                .FontColor("#333333");
        }
    }

    private static void RenderSection(ColumnDescriptor col, CvSection section)
    {
        col.Item().PaddingTop(10).Column(sectionCol =>
        {
            sectionCol.Item().Text(t =>
            {
                t.Span(section.Title.ToUpperInvariant())
                    .Bold()
                    .FontSize(11)
                    .LetterSpacing(0.08f);
            });

            sectionCol.Item().PaddingTop(1).PaddingBottom(4).Height(0.75f).Background("#000000");

            foreach (var entry in section.Entries)
                RenderEntry(sectionCol, entry);
        });
    }

    private static void RenderEntry(ColumnDescriptor col, CvEntry entry)
    {
        var hasOrg = !string.IsNullOrWhiteSpace(entry.Organization);
        var hasDate = !string.IsNullOrWhiteSpace(entry.Date);
        var hasRole = !string.IsNullOrWhiteSpace(entry.Role);
        var hasLocation = !string.IsNullOrWhiteSpace(entry.Location);

        if (!hasOrg && entry.Bullets.Count == 0) return;

        col.Item().PaddingTop(4).Column(entryCol =>
        {
            if (hasOrg)
            {
                entryCol.Item().Row(row =>
                {
                    row.RelativeItem().Text(entry.Organization).Bold().FontSize(11);
                    if (hasDate)
                        row.AutoItem().Text(entry.Date).FontSize(10);
                });
            }

            if (hasRole || hasLocation)
            {
                entryCol.Item().Row(row =>
                {
                    row.RelativeItem().Text(entry.Role).Italic().FontSize(10);
                    if (hasLocation)
                        row.AutoItem().Text(entry.Location).FontSize(10);
                });
            }

            foreach (var bullet in entry.Bullets)
            {
                entryCol.Item().PaddingTop(1).Row(row =>
                {
                    row.ConstantItem(12).Text("•").FontSize(10);
                    row.RelativeItem().Text(bullet).FontSize(10);
                });
            }
        });
    }
}
