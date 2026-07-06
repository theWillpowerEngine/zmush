# ZMUSH Reference Guide (0.0.2)

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
* -f, --folder : Set the root folder for the data files (will be created and seeded if it doesn't exist).  Default (home)/z
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

### Connecting, and Basic Concepts

Once the server is running you can connect to the URL shown in the log message that starts "Server Running!" using any web browser.  You will be presented with a login page.  You can use the user 'owner' with the password 'owner' to establish the initial connect.  This user is a special user called an 'admin' that has access to everything in the MUSH.  In general it's best not to actually login and use this user for day-to-day things, but we'll talk more about that later.  For now, to explore, it's fine.

Once you've logged in you should see that you're in a place called the Master Room, and you can start typing instructions.  In general, the first word of whatever you type is called a command, and it tells the game what you actually want to do.  For example, you could type:

    say Hello everyone!

And then press Enter.  The result will be your character saying "Hello everyone!" to anyone else who is in the same room as you.  In this case that's only you, but you'll see it at least.  If you did the above, you'll see the following in your game screen:

    Stimpy says, "Hello everyone!"

Which might have you wondering who 'Stimpy' is.  In ZMUSH, there is a difference between the User (that you log into) and the character (that you actually play in the world).  One User can have multiple Characters if you've enabled that in the Settings.  'owner' is the User you logged into, and Stimpy is the character that User uses to play the game.  Your permissions are inherited at the User level, not the Character level, so in general you won't want to create other characters on the 'owner' User unless you want them to also be admins.

There are two kinds of special commands, @-commands and !-commands.  @-commands start with an @, and usually modify game objects or perform administrative functions.  Access to these is controlled in the settings, and most users use them fairly rarely unless they're building or coding something.  !-commands start with an !, and are accessible to all users (generally -- the server owner can restrict any commands they want) performing "out of game" type actions.  Resetting your password is a common example.

Since having a username and a password of 'owner' is extremely insecure, let's experiment with an !-command right now and fix that.  Type the following:

    !password owner=some new password

Once you do this, you will change the password for the 'owner' user.  Note that the 'owner' in this line refers to your current password, not the user itself.  You can only change your own password with this command.  Admins (and often privileged users in other roles like wizards or moderators) can change another user's password like this:

    @password owner=new password

In this case 'owner' refers to the user whose password is being changed.  Since this command is generally used to help people regain access when they forget their password, it's assumed you don't have acccess to the existing password.

While you're in the client, you can scroll through your past commands with the up and down arrows.  If you want to clear the command text box, press Escape.

### Understanding the Log

Here, again, are some example log messages that you'd see in the zmush terminal window (or the log file if you're logging it to a file):

    [2334]  Load Complete.  Total ZObjects loaded: 3 in 18.3483 ms.
    [2334]  Initialization complete!  Almost there.
    [2334:net]  Starting server on port 4676 in working directory '/home/malf/z/'...

We've already explained the timestamp in square brackets, but what about the different subsystems (the bit that sometimes appears after the time with ':' separating them)?  You don't generally have to memorize these, but some of them can be important:

    * CRITICAL:  Something VERY IMPORTANT happening.  Generally only used for unusual errors that are not expected and don't have known remediations.  Also used in very specific cases (like creating the default admin user) to let you know of a potential security or access issue.

    * WARN:  Something quite important happening, but not something that implies the game itself is damaged or compromised.  These are things you should fix, but which won't ruin the player experience (for example, a ZObject having an invalid parent)

    * hax:  Security/Access Control prevented something from happening.  Could just be something messing around with commands, could be an attempt to break something or hack the game

    * net:  Related to the network server/HTTP layer

    * quota:  If the 'LogQuotaExceeds' setting is true, this will log any time something exceeds its quota, with tracing information to help you identify poorly optimized scripts or runaway ZObjects

    * zelazny:  Created from the 'log' keyword in Zelazny. 

    * cron:  Related to the scheduler (the thing that does work in the background)

### Settings, Roles and the File System

You generally won't have to edit game files directly, but it can be helpful to understand the file structure...  especially for power users.  The files and folders are:

    -root (either (home)/z or whatever you set with --folder when starting the server)
        -drv : "driver files".  Zelazny source code which runs on init.  main.z is the entry point
        -obj : All the ZObjects saved in YAML by Id
        -site : Any content put in this folder will be hosted by the HTTP server.  index.htm is the default page
        -usr : User/Login records saved in YAML by login name
        
        Version : Text file containing the version that created the folder structure
        Settings : YAML file containing the settings -- loaded on initialization, but may be overridden in the driver files if settings are sts-ed

If you want to back up your game files, the obj and usr folders are the important ones.  You can move these to a fresh ZMUSH server and basically pick up where you left off.  The entire code for the front end (HTML, JS and CSS) are freely editable in the site folder, allowing you to make any kind of UI you can think of as long as you understand the core, fairly simple, mechanism by which the front end talks to the server.  This is beyond the scope of this guide for now, but it's just basic jquery/ajax.

There are some server-level settings which you might occassionally have to modify.  The Admin user (#0) can use the 'sts' zelazny keyword to interact with these settings, which is most commonly done in the main.z file in the "driver files" directory.  Here is a very simple version of main.z from 0.0.2:

    ; This file is executed every time zmush starts.
    {do
        {sts startroom 1}
        {log "Initialization zelazny has been run successfully"}
    }

Don't worry if the syntax doesn't make sense, we'll explain it later, but in particular the line:

    {sts startroom 1}

is an example of interacting with a setting.  In this case we're setting the StartRoom setting (where new PCs start the game) to #1, which is also the Master Room.  In a real game you wouldn't want to do this most of the time, but don't worry about that for now.  The settings you can interact with in this way are:

    Name                Type        Action
    AutoLinkExits       bool        If true, exits will automatically be turned into Action Links, allowing them to be clicked on
    AutoSaveMinutes     int         Number of minutes between automatic ZObject save.  If 0 or less, ZObjects will save every time, which could impact disk life and performance under heavy use
    BreakOnException    bool        If true, the server will crash if an exception occurs.  You should basically never set this unless working on the C# code
    LogQuotaExceeds     bool        If true, any time a Zelazny evaluation is stopped because of quota it will log some useful tracing information
    MasterItem          int         ID of the Master Item, which is the default parent of every newly-created Item (default -1)
    MasterPC            int         ID of the Master PC, which is the default parent of every newly-created PC (default -1)
    MasterRoom          int         ID of the Master Room, which is the default parent of every newly-created room (default #1)
    ShowHTTP            bool        If true, will show all HTTP requests in the log (VERY spammy, but can be useful for debugging)
    StartRoom           int         ID of the room new PCs are created in

#### RBAC

ZMUSH has "Role-Based Access Control", also known as RBAC.  This allows you to finely control access to particular commands by creating your own system of user powers and entitlements.  As of the current version, this can only be modified by editing the Settings file, which is located in the root of your game directory.  This system will be expanded over time to include new functionality, but for now it is primarily used to control access to commands that can be run, and Flags that can be set.

Here is the RBAC section from the default Settings file as of 0.0.2:

    CommandPerms:
      ? - '@create'
        - '@cr'
        - '@dig'
      : - advanced
      ? - '@tel'
        - '@password'
      : - basic
    FlagPerms:
      ? - Darksight
      : - basic
      ? - Dark
      : - advanced
    Roles:
      wizard:
      - basic
      - advanced
      moderatus:
      - basic

If you don't know YAML don't mess with this, but if you do the structure is fairly discoverable.  "CommandPerms" and "FlagPerms" determine which commands and flags require special permissions to access.  By default, every command is open, so as you add them to these collections you're restricting them to particular permissions.  The two permissions in this example are 'advanced' and 'basic'.

The Roles collection determines which roles have which permissions (which in turn determines what they can do).  There is one special role, called "Admin", that can do EVERYTHING.  Stimpy, user #0, is an admin.  In general it's best to create a few other Admins as possible (ideally none), and to grant access to other functionality using this RBAC system.

I intend to add commands to make RBAC easier in the future, so stay tuned.

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
 
    allow       Id          Exit        Exit        Allow certain people (locked otherwise.  Can't combine with deny)
    deny        Id          Exit        Exit        Deny certain people (unlocked otherwise.  Can't combine with allow)


### Flags

Dark
Darksight
Handler (enables command handlers)
U1 - U10
S1 - S10

Male
Female
Neuter


### Special Attributes

#### Exits

    arriveMsg    Special Message to show when arriving (special registers:   %a %an)
    leaveMsg     Special Message to show when leaving (special registers:   %a %an)
    lockMsg      Special Message to show if the exit is locked to the person (private emit) (special registers:   %a %an)

### Handlers and Listeners

...

## First Login - Basic Tutorial

## Admin/Mod/Wizard Guide

## Command Reference

    look (l) [<obj>]

    get (g) <obj>
    drop (dr) <obj>

    inventory (inv, i)

    say <text>
    "<text>

    emote (em) <text>
    :<text>

    emote/ns <text>
    ;<text>

    @name <obj>=<name>
    @desc <obj>=<name>

    @password <user name>=<new password>

    @lock <obj>=<lock>[:<lock argument>]
    @lock/list (@lock/l) <obj>
    @unlock <obj>=<lock>[:<lock argument>]

    @flag <obj>=<flag>
    @flag <obj>=!<flag>

    @attr <object>.<attribute>=<value>
    @attr <object>.<attribute>              //Shorthand for removing an attribute, you can also @attr/clear or @attr/c
    
    @attr/val (@attr/v) <obj>.<attr>

    @attr/list (@attr/l) <obj>

    @create (@cr) <name>
        @cr/item (@cr/i)
        @cr/character (@cr/c)
        @cr/room (@cr/r)
        @cr/exit (@cr/x, @cr/ex)

        @cr/user (@cr/u) <name>[:<pwd>]=#<character id>         If no password the user name is used

    @dig <name>
    @dig/1 <name>
    @dig/t <name>
    @dig/1t <name>

    @tel <room>         Can be #id or name part

    @eval <zelazny code>

    !password <current password>=<new password>

## Zelazny Reference

    concat <val> <val>... - Combine into a single string

    do <val>... - Evaluate each value and return the last one

    emit <obj> <message> - Send a message.  Context sensitive (can send to a player, a room or even an item)

    log <val> - Logs a message with the 'zelazny' tag in the zmush log

    match <check> (<compare> <val>)... [<val>] - For each pair, if the compare value matches the check value, evaluate and return that pair's value.  The optional final value is if nothing matches

    single <val>... - Returns the first of the values that follow it that isn't empty.

    stg <setting> - Gets the Engine Setting specified (see "Settings, Roles and Permissions" earlier in this guide for more info on Engine Settings)

    string (str) <interpolated string> - Anything in [] will be evaluated as code.  For example:  @eval str "The attr is: [v here a]"

    val (v) <attr> - Get value of attribute from context
    val (v) <obj> <attr> - Get alue of attribute from object

### Predicates

There are two formats a predicate can appear in.  They are, singleton predicates:

    <pred> <val> [<true> [<false>]]

And comparison predicates:

    <pred> <val1> <val2> [<true> [<false>]]

The singleton predicates are:

    Predicate       Description
    ?=              if the value is truthy
    ?!              if the value is falsey

    ??              if the value is not empty

And the comparison predicates are:

     Predicate       Description

### Reader Shortcuts

Auto-Interpolation:

    @eval emit here `The attr is: [v here a]`
    ;translates to:
    @eval emit here {str "The attr is: [v here a]"}



### Special Zelazny Commands

These can only be evaluated in special contexts, or by particular users

    sts <setting> <val> - Sets the Engine Setting specified (see "Settings, Roles and Permissions" earlier in this guide for more info on Engine Settings)

## Built in Formatters

    {blah} - Action link (command = text)
    {blah:cmd} - Action link (text = blah, command = cmd)
    {<c> text} - Color text.  c = red, yellow, green, blue, purple

### Special Escape Characters

    %n    Newline
    %s    Non-breaking space
    %t    "tab" (4 spaces)
    %%    % as a printable character

    %1-9  Register value

    %a    Actor's object id
    %an   Actor's name
    %as   Subjective pronoun (he/she/it) %As to capitalize it
    %ao   Objective pronoun (him/her/it) %Ao to capitalize it
    %ap   Possessive pronoun (his/hers/its) %Ap to capitalize it
    %l    Actor's location

## Appendix:  Tutorial-level Snippers

@attr here.outside=sunny
It's {single {v outside} normal} here.

@flag here=handler
@attr here.$echo *=emit this %1
@attr here.$1 *:*=do {emit here %2} {emit here %1}

This is for the master room (using %a emits to the right person, not to the room itself):
@flag here=handler
@attr here.$test=emit %a "The test worked"


## Appendix:  Common Snippets and Libraries



