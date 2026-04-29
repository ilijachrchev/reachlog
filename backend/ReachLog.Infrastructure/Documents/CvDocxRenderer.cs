using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ReachLog.Infrastructure.Models;

namespace ReachLog.Infrastructure.Documents;

internal static class CvDocxRenderer
{
    private const string FontName = "Inter";
    private const int RightTabTwips = 10080;

    internal static byte[] Render(ParsedCv cv)
    {
        using var ms = new MemoryStream();

        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            body.AppendChild(NameParagraph(cv.Name));

            if (!string.IsNullOrWhiteSpace(cv.Contact))
                body.AppendChild(ContactParagraph(cv.Contact));

            foreach (var section in cv.Sections)
            {
                body.AppendChild(SectionHeaderParagraph(section.Title));

                foreach (var entry in section.Entries)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Organization))
                        body.AppendChild(EntryHeaderParagraph(entry.Organization, entry.Date));

                    if (!string.IsNullOrWhiteSpace(entry.Role))
                        body.AppendChild(EntryRoleParagraph(entry.Role, entry.Location));

                    foreach (var bullet in entry.Bullets)
                        body.AppendChild(BulletParagraph(bullet));
                }
            }

            body.AppendChild(PageSetupParagraph());
            mainPart.Document.Save();
        }

        return ms.ToArray();
    }

    private static Paragraph NameParagraph(string name)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "0", After = "60" }
        ));
        var run = new Run();
        run.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new Bold(),
            new FontSize { Val = "40" }
        ));
        run.AppendChild(new Text(name) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);
        return para;
    }

    private static Paragraph ContactParagraph(string contact)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "0", After = "120" }
        ));
        var run = new Run();
        run.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new FontSize { Val = "18" }
        ));
        run.AppendChild(new Text(contact) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);
        return para;
    }

    private static Paragraph SectionHeaderParagraph(string title)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "200", After = "60" },
            new ParagraphBorders(
                new BottomBorder
                {
                    Val = BorderValues.Single,
                    Size = 4,
                    Space = 1,
                    Color = "000000"
                }
            )
        ));
        var run = new Run();
        run.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new Bold(),
            new Caps(),
            new FontSize { Val = "22" },
            new Spacing { Val = 20 }
        ));
        run.AppendChild(new Text(title) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);
        return para;
    }

    private static Paragraph EntryHeaderParagraph(string organization, string date)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "80", After = "0" },
            new Tabs(new TabStop { Val = TabStopValues.Right, Position = RightTabTwips })
        ));

        var orgRun = new Run();
        orgRun.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new Bold(),
            new FontSize { Val = "22" }
        ));
        orgRun.AppendChild(new Text(organization) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(orgRun);

        if (!string.IsNullOrWhiteSpace(date))
        {
            para.AppendChild(new Run(new TabChar()));
            var dateRun = new Run();
            dateRun.AppendChild(new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName },
                new FontSize { Val = "20" }
            ));
            dateRun.AppendChild(new Text(date) { Space = SpaceProcessingModeValues.Preserve });
            para.AppendChild(dateRun);
        }

        return para;
    }

    private static Paragraph EntryRoleParagraph(string role, string location)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "0", After = "0" },
            new Tabs(new TabStop { Val = TabStopValues.Right, Position = RightTabTwips })
        ));

        var roleRun = new Run();
        roleRun.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new Italic(),
            new FontSize { Val = "20" }
        ));
        roleRun.AppendChild(new Text(role) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(roleRun);

        if (!string.IsNullOrWhiteSpace(location))
        {
            para.AppendChild(new Run(new TabChar()));
            var locRun = new Run();
            locRun.AppendChild(new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName },
                new FontSize { Val = "20" }
            ));
            locRun.AppendChild(new Text(location) { Space = SpaceProcessingModeValues.Preserve });
            para.AppendChild(locRun);
        }

        return para;
    }

    private static Paragraph BulletParagraph(string text)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "20", After = "0" },
            new Indentation { Left = "360" }
        ));
        var run = new Run();
        run.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new FontSize { Val = "20" }
        ));
        run.AppendChild(new Text($"• {text}") { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);
        return para;
    }

    private static Paragraph PageSetupParagraph()
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SectionProperties(
                new PageSize { Width = 12240U, Height = 15840U },
                new PageMargin { Top = 1080, Bottom = 1080, Left = 1080U, Right = 1080U }
            )
        ));
        return para;
    }
}
