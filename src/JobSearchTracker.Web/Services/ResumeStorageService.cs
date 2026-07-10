namespace JobSearchTracker.Web.Services;

public class ResumeStorageService
{
    private static readonly string[] AllowedExtensions = [".pdf", ".doc", ".docx"];

    private readonly string _rootPath;

    public ResumeStorageService(IWebHostEnvironment env, IConfiguration config)
    {
        var configuredRoot = config["ResumeStorage:RootPath"] ?? "Resumes";
        _rootPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, configuredRoot));
        Directory.CreateDirectory(_rootPath);
    }

    /// <summary>Saves the uploaded file under {Root}/{Company}/{fileName} and returns a path relative to the root.</summary>
    public async Task<string> SaveAsync(string companyName, string originalFileName, Stream content)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only PDF, DOC, or DOCX files are allowed.");

        var folder = Path.Combine(_rootPath, SanitizeSegment(companyName));
        Directory.CreateDirectory(folder);

        var safeFileName = SanitizeSegment(Path.GetFileNameWithoutExtension(originalFileName)) + extension;
        var destinationPath = GetUniquePath(Path.Combine(folder, safeFileName));

        var fullDestination = Path.GetFullPath(destinationPath);
        if (!fullDestination.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file path.");

        await using var fileStream = new FileStream(fullDestination, FileMode.CreateNew);
        await content.CopyToAsync(fileStream);

        return Path.GetRelativePath(_rootPath, fullDestination).Replace('\\', '/');
    }

    public string GetAbsolutePath(string relativePath) =>
        Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string SanitizeSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) || cleaned.All(c => c == '.') ? "Unsorted" : cleaned;
    }

    private static string GetUniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var i = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            i++;
        } while (File.Exists(candidate));
        return candidate;
    }
}
