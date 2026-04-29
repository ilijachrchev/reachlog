using System.Text.RegularExpressions;
using ReachLog.Infrastructure.Models;

namespace ReachLog.Infrastructure.Services;

internal static class CvParser
{
    private static readonly HashSet<string> SectionKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "education", "experience", "work experience", "projects", "skills",
        "technical skills", "certifications", "certification", "awards", "summary",
        "objective", "publications", "volunteer", "volunteering", "activities",
        "interests", "languages", "references", "honors", "achievements",
        "courses", "training", "work history", "professional experience",
        "research", "leadership", "open source"
    };

    private static readonly Regex DatePattern = new(
        @"(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|" +
        @"Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)" +
        @"\.?\s+\d{4}\s*[–\-]\s*(?:Present|\d{4})" +
        @"|(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|" +
        @"Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)" +
        @"\.?\s+\d{4}" +
        @"|\d{4}\s*[–\-]\s*(?:Present|\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public static ParsedCv Parse(string fullText)
    {
        var lines = fullText.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToList();
        var i = 0;

        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i])) i++;
        var name = i < lines.Count ? lines[i++].Trim() : string.Empty;

        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i])) i++;
        var contact = string.Empty;
        if (i < lines.Count && IsContactLine(lines[i]))
            contact = lines[i++].Trim();

        var sections = new List<CvSection>();
        while (i < lines.Count)
        {
            while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i])) i++;
            if (i >= lines.Count) break;

            if (!IsSectionHeader(lines[i])) { i++; continue; }

            var sectionTitle = lines[i++].Trim();
            var entries = ParseEntries(lines, ref i);
            sections.Add(new CvSection(sectionTitle, entries));
        }

        return new ParsedCv(name, contact, sections);
    }

    private static List<CvEntry> ParseEntries(List<string> lines, ref int i)
    {
        var entries = new List<CvEntry>();

        while (i < lines.Count && !IsSectionHeader(lines[i]))
        {
            while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i])) i++;
            if (i >= lines.Count || IsSectionHeader(lines[i])) break;

            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

            if (IsBullet(line))
            {
                if (entries.Count > 0)
                {
                    var last = entries[^1];
                    var appended = new List<string>(last.Bullets);
                    while (i < lines.Count && !string.IsNullOrWhiteSpace(lines[i])
                           && IsBullet(lines[i].Trim()) && !IsSectionHeader(lines[i]))
                    {
                        appended.Add(StripBullet(lines[i].Trim()));
                        i++;
                    }
                    entries[^1] = last with { Bullets = appended };
                }
                else i++;
                continue;
            }

            i++;
            var (firstText, firstDate) = ExtractDate(line);
            var organization = firstText.Trim('|', ' ');
            var date = firstDate;
            var role = string.Empty;

            if (i < lines.Count && !string.IsNullOrWhiteSpace(lines[i])
                && !IsSectionHeader(lines[i]) && !IsBullet(lines[i].Trim()))
            {
                var next = lines[i].Trim();
                var (nextText, nextDate) = ExtractDate(next);
                if (!string.IsNullOrWhiteSpace(nextDate) && string.IsNullOrEmpty(date))
                {
                    role = organization;
                    organization = nextText.Trim('|', ' ');
                    date = nextDate;
                    i++;
                }
                else if (!string.IsNullOrWhiteSpace(nextText) && string.IsNullOrEmpty(nextDate))
                {
                    role = nextText.Trim('|', ' ');
                    i++;
                }
            }

            var bullets = new List<string>();
            while (i < lines.Count && !string.IsNullOrWhiteSpace(lines[i])
                   && IsBullet(lines[i].Trim()) && !IsSectionHeader(lines[i]))
            {
                bullets.Add(StripBullet(lines[i].Trim()));
                i++;
            }

            entries.Add(new CvEntry(organization, date, role, string.Empty, bullets));
        }

        return entries;
    }

    private static (string text, string date) ExtractDate(string line)
    {
        var m = DatePattern.Match(line);
        if (!m.Success) return (line, string.Empty);

        var date = m.Value.Trim();
        var before = line[..m.Index].TrimEnd().TrimEnd('|').TrimEnd();
        var after = line[(m.Index + m.Length)..].TrimStart().TrimStart('|').TrimStart();
        var text = string.IsNullOrWhiteSpace(after) ? before : $"{before} {after}".Trim();
        return (text, date);
    }

    private static bool IsContactLine(string line)
        => line.Contains('@') || line.Contains('|')
           || Regex.IsMatch(line, @"\d{3}[.\-\s]\d{3}[.\-\s]\d{4}")
           || line.Contains("linkedin", StringComparison.OrdinalIgnoreCase)
           || line.Contains("github", StringComparison.OrdinalIgnoreCase);

    internal static bool IsSectionHeader(string line)
    {
        var t = line.Trim();
        if (string.IsNullOrWhiteSpace(t) || t.Length > 50 || t.Length < 3) return false;
        if (SectionKeywords.Contains(t)) return true;
        return t == t.ToUpperInvariant()
               && !char.IsDigit(t[0])
               && !t.Contains('@')
               && !t.Contains(':')
               && !t.Contains(',');
    }

    private static bool IsBullet(string line)
    {
        if (string.IsNullOrEmpty(line)) return false;
        return line[0] is '•' or '-' or '*' or '·' or '–' or '▪';
    }

    private static string StripBullet(string line)
    {
        if (string.IsNullOrEmpty(line)) return line;
        return line[0] is '•' or '-' or '*' or '·' or '–' or '▪' ? line[1..].TrimStart() : line;
    }
}
