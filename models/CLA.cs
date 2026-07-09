using CommandLine;

public class CLA
{
    [Option('p', "port", Default = 4676, Required = false, HelpText = "Set the port number (default 4676)")]
    public int Port { get; set; }

    [Option('r', "reset", Required = false, HelpText = "Reset the data files (they will be lost!)")]
    public bool Reset { get; set; }

    [Option('h', "headless", Required = false, HelpText = "Run without attempting any terminal input (useful for systemctl hosting)")]
    public static bool Headless { get; set; }

    [Option("http", Required = false, HelpText = "Show HTTP requests in the console log (spammy)")]
    public bool Http { get; set; }

    [Option('f', "folder", Required = false, HelpText = "Set the folder path")]
    public string? Folder { get; set; }
}