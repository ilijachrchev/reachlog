using QuestPDF.Drawing;

namespace ReachLog.Infrastructure.Documents;

public static class CvFonts
{
    public static string Family { get; private set; } = "Lato";

    public static void TryRegisterInter(string baseDirectory)
    {
        var dir = Path.Combine(baseDirectory, "Fonts");
        var files = new[] { "Inter-Regular.ttf", "Inter-Medium.ttf", "Inter-SemiBold.ttf", "Inter-Bold.ttf" };

        if (!files.All(f => File.Exists(Path.Combine(dir, f)))) return;

        foreach (var f in files)
            FontManager.RegisterFont(File.OpenRead(Path.Combine(dir, f)));

        Family = "Inter";
    }
}
