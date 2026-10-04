namespace Omnia.Client.Tests;

internal static class TestPaths
{
    public static string RepoRoot { get; } = LocateRepoRoot();

    public static string ClientFile(string relativePath) =>
        Path.Combine(RepoRoot, "src", "Omnia.Client", relativePath);

    private static string LocateRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Omnia.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root (Omnia.slnx).");
    }
}
