using System.Runtime.InteropServices;

namespace FileLinesCounter;

public static class Program
{
    private static string _path = ".";
    private static readonly Dictionary<string, FileEntry> Extensions = new();
    private static readonly List<string> ExcludeDirectories = new();

    public static async Task<int> Main(string[] args)
    {
        ParseArgs(args);

        try
        {
            var files = Directory
                .EnumerateFiles(_path, "*.*", SearchOption.AllDirectories)
                .Where(x => !string.IsNullOrEmpty(Path.GetExtension(x)));

            if (Extensions.Count != 0) files = files.Where(x => Extensions.ContainsKey(Path.GetExtension(x)));
            if (ExcludeDirectories.Count != 0) files = files.Where(x => ExcludeDirectories.All(y => !x.Contains(y)));


            foreach (var file in files.ToList())
            {
                var ext = Path.GetExtension(file);
                var count = await File.ReadLinesAsync(file).CountAsync();
                Increase(ext, count);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            return 1;
        }

        PrintOutput();

        return 0;
    }

    private static void PrintOutput()
    {
        Console.WriteLine("Total lines in files by extension:");

        var totalLines = 0;
        var totalFiles = 0;

        foreach (var (extension, fileEntry) in Extensions)
        {
            totalLines += fileEntry.Lines;
            totalFiles += fileEntry.FilesNumber;

            var line = fileEntry.Lines == 1 ? "line" : "lines";
            var file = fileEntry.FilesNumber == 1 ? "file" : "files";
            Console.WriteLine($"  {extension}: {fileEntry.Lines} {line} in {fileEntry.FilesNumber} {file}");
        }

        var totalFile = totalFiles == 1 ? "file" : "files";
        Console.WriteLine($"Total lines in all {totalFiles} {totalFile}: {totalLines}.");
    }

    private static void Increase(string extension, int count)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(Extensions, extension, out _);
        entry.Lines += count;
        entry.FilesNumber += 1;
    }

    // ReSharper disable once UnusedMember.Local
    private static void PrintSettings()
    {
        Console.WriteLine($"Path: {_path}");
        foreach (var (extension, _) in Extensions) Console.WriteLine($"Extension: {extension}");
        foreach (var dir in ExcludeDirectories) Console.WriteLine($"Exclude directory: {dir}");
    }

    private static void TryExcludeDirectory(string[] args, int parameterValueIndex)
    {
        if (parameterValueIndex >= args.Length)
        {
            Console.WriteLine("Error: Parameter -e requires a value.");
            Environment.Exit(1);
        }

        ExcludeDirectories.Add(args[parameterValueIndex]);
    }

    private static void TrySetPath(string[] args, int parameterValueIndex)
    {
        if (parameterValueIndex >= args.Length)
        {
            Console.WriteLine("Error: Parameter -p requires a value.");
            Environment.Exit(1);
        }

        _path = args[parameterValueIndex];
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: count_lines [EXTENSION]... [OPTION]...\n");
        Console.WriteLine("Count lines in files by extension in a directory recursively.\n");
        Console.WriteLine("With no EXTENSION, all files with extension are included.");
        Console.WriteLine("EXTENSION example: .txt .cs.\n");
        Console.WriteLine("  -e, --exclude  exclude all files whose full name contains the specified value");
        Console.WriteLine("  -p, --path     search path, current directory is default\n");
        Console.WriteLine("Examples:");
        Console.WriteLine("  count_lines .js .ts -e .vs/  Count lines in .js and .ts files, exclude named");
        Console.WriteLine("                               with '.vs/'.");
        Console.WriteLine("  count_lines                  Count lines in files by extension in current");
        Console.WriteLine("                               directory recursively.");
    }

    private static bool IsValidExtension(string extension)
    {
        if (!extension.StartsWith('.') || extension.Length < 2) return false;

        for (var i = 1; i < extension.Length; ++i)
        {
            if (!char.IsAsciiLetterOrDigit(extension[i])) return false;
        }

        return true;
    }

    private static void ParseArgs(string[] args)
    {
        for (var i = 0; i < args.Length; ++i)
        {
            var arg = args[i];

            switch (arg)
            {
                case "-e":
                case "--exclude":
                    TryExcludeDirectory(args, ++i);
                    break;
                case "-p":
                case "--path":
                    TrySetPath(args, ++i);
                    break;
                case "--help":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        Console.WriteLine($"Error: Unknown option '{arg}'.");
                        Console.WriteLine("Try 'count_lines --help' for more information.");
                        Environment.Exit(1);
                    }

                    if (!IsValidExtension(arg))
                    {
                        Console.WriteLine($"Error: Invalid extension '{arg}'.");
                        Console.WriteLine("Try 'count_lines --help' for more information.");
                        Environment.Exit(1);
                    }

                    Extensions.TryAdd(arg, default);
                    break;
            }
        }
    }
}