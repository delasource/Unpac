using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using LibArchive.Net;
using Spectre.Console;

AnsiConsole.Markup("[red]Un[/][cyan]pac![/] v0.1");
Console.WriteLine();

// Downloads Path
string url = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "cc.tgz");
var    stopwatch = Stopwatch.StartNew();

// Use Tar
var reader     = new LibArchiveReader(url);
var tarEntries = reader.Entries(); // List of relative paths in that archive

// Organize them in a file tree like datatyle
var root = new ArchiveFileNode("/", [], null);
foreach (var entry in tarEntries)
{
    var parentDir = root.Children;
    foreach (string fileOrDirName in entry.Name.Split('/'))
    {
        if (string.IsNullOrEmpty(fileOrDirName) || fileOrDirName == "." || fileOrDirName == "..")
            continue;

        var node = parentDir.FirstOrDefault(k => k.Name == fileOrDirName);
        if (node == null)
        {
            // TODO: Stream shall be null if it is a directory and set if it is a file.
            // (currently not implemented by LibArchive.Net)
            node = new ArchiveFileNode(fileOrDirName, [], entry.Stream);

            // TODO Insert sorted
            parentDir.Add(node);
            parentDir.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase));
        }

        parentDir = node.Children;
    }
}

AnsiConsole.WriteLine("Loaded in " + stopwatch.ElapsedMilliseconds + "ms");
// AnsiConsole.WriteLine("Press [red]q[/] to quit, [cyan]up[/] and [cyan]down[/] to navigate, [cyan]left[/] and [cyan]right[/] to switch panes, [cyan]backspace[/] to go back, [cyan]enter[/] to enter a directory or to extract a file");


string currentPath = "/";
var    currentNode = root;

while (true)
{
    var dir = (currentPath == "/"
            ? Array.Empty<ArchiveFileNode>()
            : [new ArchiveFileNode("..", [], null)]
        ).Concat(currentNode.Children);


    var fileSelect = AnsiConsole.Prompt(
        new SelectionPrompt<ArchiveFileNode>()
            .Title($"Navigate: [underline]{currentPath}[/]")
            .PageSize(25)
            .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
            .AddChoices(dir)
            .UseConverter(c => c.Name)
    );

    if (fileSelect.Name == "..")
    {
        // Get the nearest parent
        var parent = root;
        if (currentPath != "/")
        {
            var path       = currentPath.TrimEnd('/');
            var parentPath = path[..path.LastIndexOf('/')];
            foreach (string fileOrDirName in parentPath.Split('/'))
            {
                var node = parent.Children.FirstOrDefault(k => k.Name == fileOrDirName);
                if (node != null)
                    parent = node;
            }

            // if (string.IsNullOrEmpty(parentPath))
                // parentPath = "/"; // this is the root

            // Navigate up
            (currentNode, currentPath) = (parent, parentPath + "/");
        }
    }
    else
    {
        // Is it a file?
        if (fileSelect.Stream != null && false)
        {
            // TODO Implementation of parent library missing.
            /* TODO Decide what to do with that File now, i.e.
             * .json: Show json reader (utilizing Spectre.Console)
             * .md: Render basic markdown?
             * .exe: Nah we dont read that
             * .zip, .tar, etc.: Recursivly read them
             *              - maybe extract first, because memory?
             */

            Console.WriteLine("TODO: Can not read files yet.");
        }
        else
        {
            // Navigate into folder
            currentPath += fileSelect.Name + "/";
            currentNode =  fileSelect;
        }
    }
}

Console.WriteLine();
Console.WriteLine("end");
return;

record ArchiveFileNode(string Name, List<ArchiveFileNode> Children, LibArchiveReader.FileStream? Stream);
