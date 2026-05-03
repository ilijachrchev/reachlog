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

            foreach (var contactLine in cv.ContactLines)
                body.AppendChild(ContactParagraph(contactLine));

            foreach (var section in cv.Sections)
            {
                body.AppendChild(SectionHeaderParagraph(section.Title));

                foreach (var entry in section.Entries)
                {
                    if (entry.FlowText != null)
                    {
                        var flowParagraphs = entry.FlowText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .Where(p => p.Length > 0)
                            .ToList();
                        var inCerts = false;
                        var certParagraphs = new List<string>();
                        foreach (var paragraph in flowParagraphs)
                        {
                            if (paragraph.Equals("Certifications & Training", StringComparison.OrdinalIgnoreCase))
                            {
                                body.AppendChild(CertificationHeadingParagraph(paragraph));
                                inCerts = true;
                                continue;
                            }
                            if (inCerts)
                            {
                                certParagraphs.Add(paragraph);
                                continue;
                            }
                            var colonIndex = paragraph.IndexOf(':');
                            if (colonIndex > 0 && colonIndex < 60)
                            {
                                var label = paragraph[..(colonIndex + 1)];
                                var rest = paragraph[(colonIndex + 1)..];
                                body.AppendChild(LabeledFlowTextParagraph(label, rest));
                            }
                            else
                            {
                                body.AppendChild(FlowTextParagraph(paragraph));
                            }
                        }
                        if (certParagraphs.Count > 0)
                        {
                            var stitched = StitchWrappedCertifications(certParagraphs);
                            foreach (var cert in stitched)
                                body.AppendChild(CertificationEntryTable(cert));
                        }
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(entry.Organization))
                        body.AppendChild(EntryHeaderTable(entry.Organization, entry.Date, entry.Role, entry.Location));

                    foreach (var bullet in entry.Bullets)
                        body.AppendChild(BulletParagraph(bullet));

                    if (entry.SubEntries is { Count: > 0 })
                    {
                        foreach (var sub in entry.SubEntries)
                        {
                            body.AppendChild(SubEntryTitleParagraph(sub.Title, sub.Location));
                            foreach (var bullet in sub.Bullets)
                                body.AppendChild(BulletParagraph(bullet));
                        }
                    }
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

    private static Table EntryHeaderTable(string organization, string date, string role, string location)
    {
        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Width = "10080", Type = TableWidthUnitValues.Dxa },
            new TableLayout { Type = TableLayoutValues.Fixed },
            new TableBorders(
                new TopBorder { Val = BorderValues.None },
                new LeftBorder { Val = BorderValues.None },
                new BottomBorder { Val = BorderValues.None },
                new RightBorder { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None },
                new InsideVerticalBorder { Val = BorderValues.None }
            )
        ));

        var row1 = new TableRow();

        var orgCell = new TableCell();
        orgCell.AppendChild(new TableCellProperties(
            new TableCellWidth { Width = "7560", Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }
        ));
        var orgPara = new Paragraph();
        orgPara.AppendChild(new ParagraphProperties(new SpacingBetweenLines { Before = "40", After = "0" }));
        var orgRun = new Run();
        orgRun.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new Bold(),
            new FontSize { Val = "22" }
        ));
        orgRun.AppendChild(new Text(organization) { Space = SpaceProcessingModeValues.Preserve });
        orgPara.AppendChild(orgRun);
        orgCell.AppendChild(orgPara);

        var dateCell = new TableCell();
        dateCell.AppendChild(new TableCellProperties(
            new TableCellWidth { Width = "2520", Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }
        ));
        var hasDate = !string.IsNullOrWhiteSpace(date);
        if (hasDate)
        {
            var datePara = new Paragraph();
            datePara.AppendChild(new ParagraphProperties(
                new SpacingBetweenLines { Before = "80", After = "0" },
                new Justification { Val = JustificationValues.Right }
            ));
            var dateRun = new Run();
            dateRun.AppendChild(new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName },
                new FontSize { Val = "20" }
            ));
            dateRun.AppendChild(new Text(date) { Space = SpaceProcessingModeValues.Preserve });
            datePara.AppendChild(dateRun);
            dateCell.AppendChild(datePara);
        }
        else
        {
            dateCell.AppendChild(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "80", After = "0" })));
        }

        row1.AppendChild(orgCell);
        row1.AppendChild(dateCell);
        table.AppendChild(row1);

        var hasRole = !string.IsNullOrWhiteSpace(role);
        var hasLocation = !string.IsNullOrWhiteSpace(location);

        if (hasRole || hasLocation)
        {
            var row2 = new TableRow();

            var roleCell = new TableCell();
            roleCell.AppendChild(new TableCellProperties(
                new TableCellWidth { Width = "7560", Type = TableWidthUnitValues.Dxa },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }
            ));
            var rolePara = new Paragraph();
            rolePara.AppendChild(new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" }));
            var roleRun = new Run();
            roleRun.AppendChild(new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName },
                new Italic(),
                new FontSize { Val = "20" }
            ));
            roleRun.AppendChild(new Text(role) { Space = SpaceProcessingModeValues.Preserve });
            rolePara.AppendChild(roleRun);
            roleCell.AppendChild(rolePara);

            var locCell = new TableCell();
            locCell.AppendChild(new TableCellProperties(
                new TableCellWidth { Width = "2520", Type = TableWidthUnitValues.Dxa },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }
            ));
            var locPara = new Paragraph();
            locPara.AppendChild(new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0" },
                new Justification { Val = JustificationValues.Right }
            ));
            var locRun = new Run();
            locRun.AppendChild(new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName },
                new Italic(),
                new FontSize { Val = "20" }
            ));
            locRun.AppendChild(new Text(location) { Space = SpaceProcessingModeValues.Preserve });
            locPara.AppendChild(locRun);
            locCell.AppendChild(locPara);

            row2.AppendChild(roleCell);
            row2.AppendChild(locCell);
            table.AppendChild(row2);
        }

        return table;
    }

    private static Paragraph SubEntryTitleParagraph(string title, string? location)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "60", After = "0" },
            new Tabs(new TabStop { Val = TabStopValues.Right, Position = RightTabTwips })
        ));

        var titleRun = new Run();
        titleRun.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new Bold(),
            new FontSize { Val = "20" }
        ));
        titleRun.AppendChild(new Text(title) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(titleRun);

        if (!string.IsNullOrEmpty(location))
        {
            para.AppendChild(new Run(new TabChar()));
            var locRun = new Run();
            locRun.AppendChild(new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName },
                new Italic(),
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

    private static Paragraph LabeledFlowTextParagraph(string label, string rest)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "0", After = "40" }
        ));
        var labelRun = new Run();
        labelRun.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new Bold(),
            new FontSize { Val = "20" }
        ));
        labelRun.AppendChild(new Text(label) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(labelRun);
        var restRun = new Run();
        restRun.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new FontSize { Val = "20" }
        ));
        restRun.AppendChild(new Text(rest) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(restRun);
        return para;
    }

    private static Paragraph CertificationHeadingParagraph(string text)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "120", After = "40" }
        ));
        var run = new Run();
        run.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new Italic(),
            new FontSize { Val = "20" }
        ));
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);
        return para;
    }

    private static Table CertificationEntryTable(string line)
    {
        var splitIdx = DocxFindCertificationSplit(line);
        var nameText = splitIdx >= 0 ? line[..splitIdx].Trim() : line;
        var sourceText = splitIdx >= 0 ? line[splitIdx..].Trim() : string.Empty;

        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Width = "10080", Type = TableWidthUnitValues.Dxa },
            new TableLayout { Type = TableLayoutValues.Fixed },
            new TableBorders(
                new TopBorder { Val = BorderValues.None },
                new LeftBorder { Val = BorderValues.None },
                new BottomBorder { Val = BorderValues.None },
                new RightBorder { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None },
                new InsideVerticalBorder { Val = BorderValues.None }
            )
        ));

        var row = new TableRow();

        var nameCell = new TableCell();
        nameCell.AppendChild(new TableCellProperties(
            new TableCellWidth { Width = "7560", Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }
        ));
        var namePara = new Paragraph();
        namePara.AppendChild(new ParagraphProperties(new SpacingBetweenLines { Before = "40", After = "0" }));
        var nameRun = new Run();
        nameRun.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new Bold(),
            new FontSize { Val = "20" }
        ));
        nameRun.AppendChild(new Text(nameText) { Space = SpaceProcessingModeValues.Preserve });
        namePara.AppendChild(nameRun);
        nameCell.AppendChild(namePara);

        var sourceCell = new TableCell();
        sourceCell.AppendChild(new TableCellProperties(
            new TableCellWidth { Width = "2520", Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }
        ));
        var sourcePara = new Paragraph();
        sourcePara.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "40", After = "0" },
            new Justification { Val = JustificationValues.Right }
        ));
        if (!string.IsNullOrEmpty(sourceText))
        {
            var sourceRun = new Run();
            sourceRun.AppendChild(new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName },
                new Italic(),
                new FontSize { Val = "20" }
            ));
            sourceRun.AppendChild(new Text(sourceText) { Space = SpaceProcessingModeValues.Preserve });
            sourcePara.AppendChild(sourceRun);
        }
        sourceCell.AppendChild(sourcePara);

        row.AppendChild(nameCell);
        row.AppendChild(sourceCell);
        table.AppendChild(row);

        return table;
    }

    private static readonly System.Text.RegularExpressions.Regex DocxCertSplitPattern =
        new(@"\s(?=[A-Z][a-z]+(?:,|\s)).*\d{4}\b", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex DocxCertEndsWithYearPattern =
        new(@"\d{4}\)?$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex DocxCertNextContainsYearPattern =
        new(@"\d{4}\b", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static int DocxFindCertificationSplit(string line)
    {
        var multiSpace = line.IndexOf("  ");
        if (multiSpace > 0) return multiSpace;

        var match = DocxCertSplitPattern.Match(line);
        if (match.Success) return match.Index;

        return -1;
    }

    private static List<string> StitchWrappedCertifications(List<string> paragraphs)
    {
        var result = new List<string>();
        var i = 0;
        while (i < paragraphs.Count)
        {
            var current = paragraphs[i];
            while (i + 1 < paragraphs.Count && IsCertificationContinuation(current, paragraphs[i + 1]))
            {
                current = current.TrimEnd() + " " + paragraphs[i + 1].TrimStart();
                i++;
            }
            result.Add(current);
            i++;
        }
        return result;
    }

    private static bool IsCertificationContinuation(string current, string next)
    {
        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(next))
            return false;

        var trimmed = current.TrimEnd();
        var endsWithComma = trimmed.EndsWith(',');
        var endsWithYear = DocxCertEndsWithYearPattern.IsMatch(trimmed);
        var endsWithTerminal = trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?');

        if (endsWithYear || endsWithTerminal) return false;
        if (endsWithComma) return true;

        var nextLooksLikeContinuation =
            next.Length < 50 ||
            DocxCertNextContainsYearPattern.IsMatch(next);

        return nextLooksLikeContinuation;
    }

    private static Paragraph FlowTextParagraph(string text)
    {
        var para = new Paragraph();
        para.AppendChild(new ParagraphProperties(
            new SpacingBetweenLines { Before = "0", After = "80" }
        ));
        var run = new Run();
        run.AppendChild(new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName },
            new FontSize { Val = "20" }
        ));
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
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
