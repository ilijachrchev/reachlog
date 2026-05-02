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
        @"Aug(?:ust)?|Sep(?:t(?:ember)?)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)" +
        @"\.?\s+\d{4}" +
        @"(?:\s*[-–]\s*(?:Present|Current|\d{4}|" +
        @"(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|" +
        @"Aug(?:ust)?|Sep(?:t(?:ember)?)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\.?\s+\d{4}))?" +
        @"|\d{4}\s*[-–]\s*(?:Present|Current|\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex WrappedDateEnd = new(
        @"\d{4}\s*[-–]\s*(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|" +
        @"Aug(?:ust)?|Sep(?:t(?:ember)?)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\.?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex DateRangeHyphen = new(@"\s+-\s+", RegexOptions.Compiled);

    private static readonly Regex LocationSuffix = new(
        @"\s+([A-Z][a-zA-ZÀ-ž]+(?:,\s*[A-Z][a-zA-ZÀ-ž]+)+)\s*$",
        RegexOptions.Compiled
    );

    private static readonly Regex SingleWordLocationSuffix = new(
        @"\s+([A-Z][a-zA-ZÀ-ž]+)\s*$",
        RegexOptions.Compiled
    );

    public static ParsedCv Parse(string fullText)
    {
        var lines = fullText.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToList();
        var i = 0;

        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i])) i++;
        var name = i < lines.Count ? lines[i++].Trim() : string.Empty;

        while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i])) i++;
        var contactLines = new List<string>();
        while (i < lines.Count && !string.IsNullOrWhiteSpace(lines[i]) && IsContactLine(lines[i]) && !IsSectionHeader(lines[i]))
            contactLines.Add(lines[i++].Trim());

        var sections = new List<CvSection>();
        while (i < lines.Count)
        {
            while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i])) i++;
            if (i >= lines.Count) break;

            if (!IsSectionHeader(lines[i])) { i++; continue; }

            var sectionTitle = NormalizeSectionTitle(lines[i++].Trim());
            var entries = ParseEntries(lines, ref i, sectionTitle);
            sections.Add(new CvSection(sectionTitle, entries));
        }

        return new ParsedCv(name, contactLines, sections);
    }

    private static List<CvEntry> ParseEntries(List<string> lines, ref int i, string sectionTitle)
    {
        var sectionLines = new List<string>();
        while (i < lines.Count && !IsSectionHeader(lines[i]))
            sectionLines.Add(lines[i++].Trim());

        if (IsSkillsSection(sectionTitle))
        {
            var flowLines = sectionLines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (flowLines.Count == 0) return new List<CvEntry>();
            return new List<CvEntry>
            {
                new(string.Empty, string.Empty, string.Empty, string.Empty,
                    Array.Empty<string>(), string.Join("\n", flowLines))
            };
        }

        return ParseEntryLines(StitchWrappedDates(sectionLines));
    }

    private static List<string> StitchWrappedDates(List<string> lines)
    {
        var result = new List<string>(lines.Count);
        for (var k = 0; k < lines.Count; k++)
        {
            if (k + 1 < lines.Count && !string.IsNullOrWhiteSpace(lines[k]) && WrappedDateEnd.IsMatch(lines[k]))
            {
                var nextLine = lines[k + 1].Trim();
                var yearMatch = Regex.Match(nextLine, @"\b(\d{4})\s*$");
                if (yearMatch.Success)
                {
                    result.Add(lines[k] + " " + yearMatch.Value.Trim());
                    var remainder = nextLine[..yearMatch.Index].Trim().TrimEnd(',').Trim();
                    if (!string.IsNullOrWhiteSpace(remainder))
                        result.Add(remainder);
                    k++;
                    continue;
                }
            }
            result.Add(lines[k]);
        }
        return result;
    }

    private static List<CvEntry> ParseEntryLines(List<string> lines)
    {
        var entries = new List<CvEntry>();
        var j = 0;

        while (j < lines.Count)
        {
            if (string.IsNullOrWhiteSpace(lines[j])) { j++; continue; }

            var line = lines[j];

            if (IsBullet(line))
            {
                if (entries.Count > 0)
                {
                    var last = entries[^1];
                    if (last.SubEntries is { Count: > 0 })
                    {
                        var subList = new List<CvSubEntry>(last.SubEntries);
                        var lastSub = subList[^1];
                        subList[^1] = lastSub with { Bullets = new List<string>(lastSub.Bullets) { StripBullet(line) } };
                        entries[^1] = last with { SubEntries = subList };
                    }
                    else
                    {
                        entries[^1] = last with { Bullets = new List<string>(last.Bullets) { StripBullet(line) } };
                    }
                }
                j++;
                continue;
            }

            var (text, date) = ExtractDate(line);

            if (string.IsNullOrEmpty(date))
            {
                if (entries.Count > 0)
                {
                    var last = entries[^1];
                    if (string.IsNullOrEmpty(last.Role))
                    {
                        if (IsTitleContinuation(line))
                            entries[^1] = last with { Organization = (last.Organization + " " + line).Trim() };
                        else
                        {
                            var (role, loc) = SplitRoleAndLocation(line);
                            entries[^1] = last with { Role = role, Location = loc };
                        }
                    }
                    else if (IsSubEntryHeader(line))
                    {
                        var (subTitle, subLoc) = SplitRoleAndLocation(line);
                        var subList = last.SubEntries != null ? new List<CvSubEntry>(last.SubEntries) : new List<CvSubEntry>();
                        subList.Add(new CvSubEntry(subTitle, string.IsNullOrEmpty(subLoc) ? null : subLoc, new List<string>()));
                        entries[^1] = last with { SubEntries = subList };
                    }
                    else if (last.SubEntries is { Count: > 0 })
                    {
                        var subList = new List<CvSubEntry>(last.SubEntries);
                        var lastSub = subList[^1];
                        subList[^1] = lastSub with { Bullets = new List<string>(lastSub.Bullets) { line } };
                        entries[^1] = last with { SubEntries = subList };
                    }
                    else
                    {
                        entries[^1] = last with { Bullets = new List<string>(last.Bullets) { line } };
                    }
                }
                j++;
                continue;
            }

            j++;
            var organization = text.Trim('|', ' ');
            var entryRole = string.Empty;
            var entryLocation = string.Empty;

            while (j < lines.Count && !string.IsNullOrWhiteSpace(lines[j]) && !IsBullet(lines[j]))
            {
                var nextLine = lines[j];
                var (nextText, nextDate) = ExtractDate(nextLine);
                if (!string.IsNullOrEmpty(nextDate)) break;

                if (string.IsNullOrEmpty(entryRole))
                {
                    if (IsTitleContinuation(nextLine))
                        organization = (organization + " " + nextLine).Trim();
                    else
                        (entryRole, entryLocation) = SplitRoleAndLocation(nextText.Trim('|', ' '));
                    j++;
                }
                else break;
            }

            var bullets = new List<string>();
            while (j < lines.Count && !string.IsNullOrWhiteSpace(lines[j]) && IsBullet(lines[j]))
            {
                bullets.Add(StripBullet(lines[j]));
                j++;
            }

            entries.Add(new CvEntry(organization, date, entryRole, entryLocation, bullets));
        }

        for (var k = 0; k < entries.Count; k++)
        {
            var e = entries[k];
            var mergedBullets = MergeBullets(new List<string>(e.Bullets));
            if (e.SubEntries is { Count: > 0 })
            {
                var mergedSubs = e.SubEntries.Select(se => se with { Bullets = MergeBullets(new List<string>(se.Bullets)) }).ToList();
                entries[k] = e with { Bullets = mergedBullets, SubEntries = mergedSubs };
            }
            else
            {
                entries[k] = e with { Bullets = mergedBullets };
            }
        }

        return entries;
    }

    private static (string text, string date) ExtractDate(string line)
    {
        var m = DatePattern.Match(line);
        if (!m.Success) return (line, string.Empty);

        var date = DateRangeHyphen.Replace(m.Value.Trim(), " – ");
        var before = line[..m.Index].TrimEnd().TrimEnd('|').TrimEnd();
        var after = line[(m.Index + m.Length)..].TrimStart().TrimStart('|').TrimStart();
        var text = string.IsNullOrWhiteSpace(after) ? before : $"{before} {after}".Trim();
        return (text, date);
    }

    private static (string role, string location) SplitRoleAndLocation(string line)
    {
        var m = LocationSuffix.Match(line);
        if (m.Success) return (line[..m.Index].Trim(), m.Groups[1].Value.Trim());

        var sw = SingleWordLocationSuffix.Match(line);
        if (sw.Success)
        {
            var candidate = sw.Groups[1].Value;
            var before = line[..sw.Index].TrimEnd();
            if (before.EndsWith(candidate, StringComparison.OrdinalIgnoreCase))
                return (before, candidate);
        }

        return (line, string.Empty);
    }

    private static bool IsSubEntryHeader(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return false;
        if (trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?') || trimmed.EndsWith(','))
            return false;
        if (DatePattern.IsMatch(trimmed)) return false;
        var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2 || words.Length > 12) return false;
        var upperCount = words.Count(w => w.Length > 0 && char.IsUpper(w[0]));
        return (double)upperCount / words.Length >= 0.6;
    }

    private static List<string> MergeBullets(List<string> bullets)
    {
        var merged = new List<string>();
        var buffer = string.Empty;
        foreach (var line in bullets)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            buffer = string.IsNullOrEmpty(buffer) ? line : buffer + " " + line;
            if (buffer[^1] is '.' or '!' or '?')
            {
                merged.Add(buffer);
                buffer = string.Empty;
            }
        }
        if (!string.IsNullOrEmpty(buffer))
            merged.Add(buffer);
        return merged;
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
        var normalized = NormalizeSectionTitle(t);
        if (SectionKeywords.Contains(normalized)) return true;
        return normalized == normalized.ToUpperInvariant()
               && !char.IsDigit(normalized[0])
               && !normalized.Contains('@')
               && !normalized.Contains(':')
               && !normalized.Contains(',');
    }

    private static string NormalizeSectionTitle(string raw)
    {
        var trimmed = raw.Trim();
        if (string.IsNullOrEmpty(trimmed)) return trimmed;
        var collapsed = Regex.Replace(trimmed, @"^([A-Z])\s+([A-Z])", "$1$2");
        collapsed = Regex.Replace(collapsed, @"^([A-Z])\s+([A-Z]{2,})", "$1$2");
        return collapsed;
    }

    private static bool IsSkillsSection(string title)
        => title.IndexOf("SKILLS", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsTitleContinuation(string line)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 6) return false;
        return line.Contains(',') || Regex.IsMatch(line, @"\b\d{4}\b");
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
