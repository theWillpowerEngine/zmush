using CommandLine;

public class CLA
{
    [Option('p', "port", Default = 4676, Required = false, HelpText = "Set the port number (default 4676)")]
    public int Port { get; set; }

    [Option('r', "reset", Required = false, HelpText = "Reset the data files (they will be lost!)")]
    public bool Reset { get; set; }

    [Option('h', "http", Required = false, HelpText = "Show HTTP requests in the console log (spammy)")]
    public bool Http { get; set; }
}