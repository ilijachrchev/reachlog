using QuestPDF.Drawing;

namespace ReachLog.Infrastructure.Documents;

public static class CvFonts
{
    public static string Family { get; private set; } = "Lato";

    public static void TryRegisterInter(string baseDirectory)
    {
        var dir = Path.Combine(baseDirectory, "Fonts");
        if (!Directory.Exists(dir)) return;

        var candidates = new[]
        {
            "Inter_18pt-Regular.ttf",
            "Inter_18pt-Italic.ttf",
            "Inter_18pt-Medium.ttf",
            "Inter_18pt-MediumItalic.ttf",
            "Inter_18pt-SemiBold.ttf",
            "Inter_18pt-SemiBoldItalic.ttf",
            "Inter_18pt-Bold.ttf",
            "Inter_18pt-BoldItalic.ttf",
            "Inter-Regular.ttf",
            "Inter-Italic.ttf",
            "Inter-Medium.ttf",
            "Inter-MediumItalic.ttf",
            "Inter-SemiBold.ttf",
            "Inter-SemiBoldItalic.ttf",
            "Inter-Bold.ttf",
            "Inter-BoldItalic.ttf"
        };

        var existing = candidates
            .Select(f => Path.Combine(dir, f))
            .Where(File.Exists)
            .ToList();

        if (existing.Count == 0) return;

        foreach (var path in existing)
        {
            using var stream = File.OpenRead(path);
            FontManager.RegisterFont(stream);
        }

        Family = "Inter";
    }
}
