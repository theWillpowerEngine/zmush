using CommandLine;

var serverRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/z";
//Engine.Settings.OverrideSiteDirectory = "/home/malf/code/zmush/res/site";     //Uncomment this when editing site files for testing, it makes reloading ez.

CLA? opts = null;

Parser.Default.ParseArguments<CLA>(args).WithParsed<CLA>(o => { opts = o; });

if (opts == null) return;

serverRoot = opts.Folder ?? serverRoot;
if (opts.Reset)
{
    if (Directory.Exists(serverRoot))
    {
        Directory.Delete(serverRoot, true);
        Console.WriteLine("Data files have been reset from command line argument.");
        Console.WriteLine();
    }
}
if (opts.Http) Engine.Settings.ShowHttpRequest = true;

Engine.InitDirectories(serverRoot);
Engine.Init();

Engine.Run(opts.Port);