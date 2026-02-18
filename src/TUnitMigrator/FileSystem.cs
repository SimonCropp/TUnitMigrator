static class FileSystem
{
    public static List<string> FindProjectRoots(string directory)
    {
        // If the target directory itself contains .git, it's the only project root
        if (Directory.Exists(Path.Combine(directory, ".git")))
        {
            return [directory];
        }

        // Otherwise, look for immediate subdirectories containing .git
        var roots = new List<string>();
        foreach (var subdir in Directory.EnumerateDirectories(directory))
        {
            if (Directory.Exists(Path.Combine(subdir, ".git")))
            {
                roots.Add(subdir);
            }
        }

        return roots;
    }

    public static IEnumerable<string> EnumerateFiles(string directory, string pattern)
    {
        var stack = new Stack<string>();
        stack.Push(directory);

        while (stack.TryPop(out var current))
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(current, pattern, SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                files = [];
            }

            foreach (var file in files)
            {
                yield return file;
            }

            IEnumerable<string> subdirectories;
            try
            {
                subdirectories = Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                subdirectories = [];
            }

            foreach (var subdirectory in subdirectories)
            {
                stack.Push(subdirectory);
            }
        }
    }

    public static string? FindSolutionFile(string directory)
    {
        // Prefer .slnx over .sln
        var slnx = Directory.EnumerateFiles(directory, "*.slnx", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (slnx != null)
        {
            return slnx;
        }

        return Directory.EnumerateFiles(directory, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
    }
}
