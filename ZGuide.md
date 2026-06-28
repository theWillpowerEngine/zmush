# ZMUSH Reference Guide (0.0.1)

## Building, Configuring and Running the Server

Make sure you have git installed, and the ability to build .net core 10.0 applications (you'll need dotnet itself, and the 10.0 SDK).

Run the following commands:

    git clone https://github.com/theWillpowerEngine/zmush.git
    cd zmush
    dotnet build

    cd bin/Debug
    ./zmush

The server is now running.  You can shut it down by pressing 'x', or shut it down and delete all data (which will cause it to restore the defaults) by pressing '\'.

The server supports several command line arguments (CLAs).  You can add --help when running the server to see a full list of them.  The most important ones are:

* -p, --port : Set the port number (default 4676)
* -r, --reset : reset all data files (back them up or they're gone forever!)

While the server is running you will see a running log, resembling the following:

    [2334]  Created root directory '/home/malf/z/', driver will be the system default.  Initializing for version 0.0.1.
    [2334]  No driver directory found.  Initializing to defaults...
    [2334]  Default driver created at '/home/malf/z/drv/default.z'.
    [2334]  No object directory found.  Initializing to defaults...
    [2334]  No player directory found.  Initializing to defaults...
    [2334:CRITICAL]  Created admin user.  login: owner, password: owner
    [2334]  No HTML root directory found.  Initializing to default site (hope you like it ugly)
    [2334]  Site content is loaded and cached.
    [2334]  Loading 3 ZObjects...
    [2334]  Load Complete.  Total ZObjects loaded: 3 in 18.3483 ms.
    [2334]  Initialization complete!  Almost there.
    [2334:net]  Starting server on port 4676 in working directory '/home/malf/z/'...
    [2334:net]  Server running!  You can now access the server at http://localhost:4676
    [2334]  Press 'X' to stop the server, or 'R' to restart it.  'L' will reload the site content without disrupting the server.  '\' will shut down AND delete all your files so don't press that unless you really want to.

The thing in brackets is a time stamp (hour and then minute in 24h time).  This log will get very int over time and consistent use, but it can be very helpful when troubleshooting things or trying to figure out something that happened on the server.  There are extra levels of detail you can turn on with CLA, including HTTP requests (some nerd shit, don't worry about it if you don't know what it is), etc.

This output can be logged to a file using your terminal system.  This has high utility value of course, allowing you to reference logs and deal with troublesome scenarios, but it can consume a lot of disk space and may require periodic cleanup.  I recommend avoiding this until you're a power user, and then using it selectively as needed.  

### Understanding the Log

CRITICAL
hax
WARN

### Settings, Roles and Permissions

...


## Rooms, Items, Characters and ZObjects in general

Everything in ZMUSH is a ZObject, which is a fancy way of saying "a thing in the game world".  Rooms, items, exits and characters are all ZObjects.  

...

### Anatomy of a ZObject

#### Inheritance / Master ZObjects

### How to Find Things

Id or partial name syntax.  Object hierarchy

Special commands

### Understanding Locks

By default only the owner of a ZObject or an admin with elevated permissions can edit and modify ZObjects.  There are ways to adjust these permissions, called locks.  Locks can have other effects as well, but one of their most common use cases is controlling access to a ZObject.

If you want to let anyone edit something, you can add the "public" lock.  This basically turns off security.  If you would like to enable particular users to edit the ZObject, you can add "pc" locks, with an argument of the user's ZObject Id.  You can have multiple pc locks on the same ZObject.  If you have setup a more complex permission system and would like to temporarily disable all but the owner and admin access you can add the "full" lock.

You can use the @lock command family to manipulate locks on ZObjects.

The locks detailed above are the "access" locks, which can go on any ZObject and effect who has access to them.  There are other kinds of locks as well.



#### Lock Quick Reference

    Lock        Arg        Type(s)      Category    Function
    public      -           -           Access      Disable normal security, anyone can edit
    pc          Id          -           Access      Allow certain characters access to the ZObject
    full        -           -           Access      Overrides any other acccess-level locks and returns to default (only owner + admins)

    fixed       -           Item        Item        Only those with access to the ZObject can pick it up or drop it
    static      -           Item        Item        Cannot be picked up or dropped (the static lock has to be removed first)

## First Login - Basic Tutorial

## Admin/Mod/Wizard Guide

## Command Reference

    look (l) [<obj>]

    get (g) <obj>
    drop (dr) <obj>

    inventory (inv, i)

    @name <obj>=<name>
    @desc <obj>=<name>

    @lock <obj>=<lock>[:<lock argument>]
    @lock/list (@lock/l) <obj>
    @unlock <obj>=<lock>[:<lock argument>]

    @create (@cr) <name>
        @cr/item (@cr/i)
        @cr/character (@cr/c)
        @cr/room (@cr/r)
        @cr/exit (@cr/x, @cr/ex)

    @dig <name>
    @dig/1 <name>

    @tel <room>         Can be #id or name part

## Zelazny Reference

## Appendix:  Common Snippets and Libraries



