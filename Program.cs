using CommandLine;

var serverRoot = "/home/malf/z";

CLA opts = null;

Parser.Default.ParseArguments<CLA>(args).WithParsed<CLA>(o => { opts = o; });

if (opts == null) return;
if (opts.Reset)
{
    if (Directory.Exists(serverRoot))
    {
        Directory.Delete(serverRoot, true);
        Console.WriteLine("Data files have been reset from command line argument.");
        Console.WriteLine();
    }
}

Engine.InitDirectories(serverRoot);

Engine.Run(opts.Port);