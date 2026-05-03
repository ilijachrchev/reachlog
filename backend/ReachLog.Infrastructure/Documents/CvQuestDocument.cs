using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReachLog.Infrastructure.Models;

namespace ReachLog.Infrastructure.Documents;

internal sealed class CvQuestDocument : IDocument
{
    private static readonly System.Text.RegularExpressions.Regex CertSplitPattern =
        new(@"\s(?=[A-Z][a-z]+(?:,|\s)).*\d{4}\b", System.Text.RegularExpressions.RegexOptions.Compiled);

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

        foreach (var contactLine in _cv.ContactLines)
        {
            col.Item()
                .PaddingTop(3)
                .Text(contactLine)
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
        if (entry.FlowText != null)
        {
            var paragraphs = entry.FlowText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            col.Item().PaddingTop(2).Column(flowCol =>
            {
                var inCertificationsBlock = false;

                foreach (var paragraph in paragraphs)
                {
                    if (paragraph.Equals("Certifications & Training", StringComparison.OrdinalIgnoreCase))
                    {
                        flowCol.Item().PaddingTop(6).Text(paragraph).Italic().FontSize(10);
                        inCertificationsBlock = true;
                        continue;
                    }

                    if (inCertificationsBlock)
                    {
                        RenderCertificationEntry(flowCol, paragraph);
                        continue;
                    }

                    var colonIndex = paragraph.IndexOf(':');
                    if (colonIndex > 0 && colonIndex < 60)
                    {
                        var label = paragraph.Substring(0, colonIndex + 1);
                        var rest = paragraph.Substring(colonIndex + 1);
                        flowCol.Item().PaddingBottom(2).Text(t =>
                        {
                            t.Span(label).SemiBold().FontSize(10);
                            t.Span(rest).FontSize(10);
                        });
                    }
                    else
                    {
                        flowCol.Item().PaddingBottom(2).Text(paragraph).FontSize(10);
                    }
                }
            });
            return;
        }

        var hasOrg = !string.IsNullOrWhiteSpace(entry.Organization);
        var hasDate = !string.IsNullOrWhiteSpace(entry.Date);
        var hasRole = !string.IsNullOrWhiteSpace(entry.Role);
        var hasLocation = !string.IsNullOrWhiteSpace(entry.Location);

        if (!hasOrg && entry.Bullets.Count == 0) return;

        col.Item().PaddingTop(2).Column(entryCol =>
        {
            if (hasOrg)
            {
                entryCol.Item().Row(row =>
                {
                    row.RelativeItem().Text(entry.Organization).Bold().FontSize(11);
                    row.ConstantItem(180).AlignRight().Text(entry.Date).FontSize(10);
                });
            }
            
            Console.WriteLine($"[CvQuest] org={entry.Organization} | role={entry.Role} | location={entry.Location} | hasRole={hasRole}");
            if (hasRole || hasLocation)
            {
                entryCol.Item().Row(row =>
                {
                    row.RelativeItem().Text(entry.Role).FontFamily(CvFonts.Family).Italic().FontSize(10);
                    row.ConstantItem(180).AlignRight().Text(entry.Location).FontFamily(CvFonts.Family).Italic().FontSize(10);
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

            if (entry.SubEntries is { Count: > 0 })
            {
                foreach (var sub in entry.SubEntries)
                {
                    entryCol.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text(sub.Title).Bold().FontSize(10);
                        if (!string.IsNullOrEmpty(sub.Location))
                            row.ConstantItem(150).AlignRight().Text(sub.Location).FontFamily(CvFonts.Family).Italic().FontSize(10);
                    });
                    foreach (var bullet in sub.Bullets)
                    {
                        entryCol.Item().PaddingTop(1).Row(row =>
                        {
                            row.ConstantItem(12).Text("•").FontSize(10);
                            row.RelativeItem().Text(bullet).FontSize(10);
                        });
                    }
                }
            }
        });
    }

    private static void RenderCertificationEntry(ColumnDescriptor col, string line)
    {
        var splitIdx = FindCertificationSplit(line);
        if (splitIdx < 0)
        {
            col.Item().PaddingTop(2).Text(line).FontSize(10);
            return;
        }

        var name = line[..splitIdx].Trim();
        var source = line[splitIdx..].Trim();

        col.Item().PaddingTop(2).Row(row =>
        {
            row.RelativeItem().Text(name).SemiBold().FontSize(10).FontFamily(CvFonts.Family);
            row.ConstantItem(220).AlignRight().Text(source).Italic().FontSize(10).FontFamily(CvFonts.Family);
        });
    }

    private static int FindCertificationSplit(string line)
    {
        var multiSpace = line.IndexOf("  ");
        if (multiSpace > 0) return multiSpace;

        var match = CertSplitPattern.Match(line);
        if (match.Success) return match.Index;

        return -1;
    }
}
