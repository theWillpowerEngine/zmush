# ZMUSH Reference Guide (0.0.3)

## Building, Configuring and Running the Server

### Setting up on Linux the Right Way

This approach will setup zmush as a systemctl daemon, meaning it will always quietly run in the background and not be bothersome.  This also gives you access to the awesome journalctl.  If you're on windows idk try WSL or docker or something.

    sudo dnf install git dotnet-sdk-10.0

    cd ~
    git clone https://github.com/theWillpowerEngine/zmush.git
    cd zmush
    dotnet build

    cd /etc/systemd/system
    nano zmush.service

Now in nano, paste/type the following:

    [Unit]
    Description=zmush
    After=network.target

    [Service]
    Type=simple
    ExecStart=/root/zmush/bin/Debug/zmush -h
    Restart=always
    User=youruser
    WorkingDirectory=/root/z
    StandardInput=null

    [Install]
    WantedBy=multi-user.target 

Note that you should make sure ExecStart is the correct path.  The WorkingDirectory doesn't really matter.

Next let's have systemctl start zmush:

    sudo systemctl daemon-reload
    sudo systemctl enable --now zmush

You can access the logs via journalctl, which is awesome but beyond the scope of this guide (honestly this whole section is but I'm nice).  Here are some useful incantations:

    journalctl -u zmush -f          # follow, like tail -f
    journalctl -u zmush -n 100      # last 100 lines
    journalctl -u zmush -o cat      # strip timestamps/metadata, just the raw output
    journalctl -u zmush --since today

If you are getting weird "Permission Denied" errors when starting the service and you're on Fedora, run these commands:

    sudo dnf install policycoreutils-python-utils
    sudo semanage fcontext -a -t bin_t "/path/to/app(/.*)?"
    sudo restorecon -Rv /path/to/app

If you're anal-retentive and don't want to mess with SELinux without confirming this is the cause, look for AVC denials in:

    sudo ausearch -m avc -ts recent

GLHF!

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

    ... Copy from readme

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

There is a built in role (admin) that can do anything.  There is also a built in role (server), to allow access to the @server commands, which are otherwise totally admin limited for safety.

If you don't know YAML don't mess with this, but if you do the structure is fairly discoverable.  "CommandPerms" and "FlagPerms" determine which commands and flags require special permissions to access.  By default, every command is open, so as you add them to these collections you're restricting them to particular permissions.  The two permissions in this example are 'advanced' and 'basic'.

The Roles collection determines which roles have which permissions (which in turn determines what they can do).  There is one special role, called "Admin", that can do EVERYTHING.  Stimpy, user #0, is an admin.  In general it's best to create a few other Admins as possible (ideally none), and to grant access to other functionality using this RBAC system.

I intend to add commands to make RBAC easier in the future, so stay tuned.

## Rooms, Items, Characters and ZObjects in general

Everything in ZMUSH is a ZObject, which is a fancy way of saying "a thing in the game world".  Rooms, items, exits and characters are all ZObjects.

...

### Anatomy of a ZObject

Id, Name, Desc, Parent, Owner, 

Flags, Attrs, Locks, Subrealities

Quota

#### Inheritance / Master ZObjects

### How to Find Things

Id or partial name syntax.  Object hierarchy

Special commands

### Understanding Locks

By default only the owner of a ZObject or an admin with elevated permissions can edit and modify ZObjects.  There are ways to adjust these permissions, called locks.  Locks can have other effects as well, but one of their most common use cases is controlling access to a ZObject.

If you want to let anyone edit something, you can add the "public" lock.  This basically turns off security.  If you would like to enable particular users to edit the ZObject, you can add "id" locks, with an argument of the user's ZObject Id.  You can have multiple pc locks on the same ZObject.  If you have setup a more complex permission system and would like to temporarily disable all but the owner and admin access you can add the "full" lock.

You can use the @lock command family to manipulate locks on ZObjects.

The locks detailed above are the "access" locks, which can go on any ZObject and effect who has access to them.  There are other kinds of locks as well.

### Handlers and Listeners

...

## First Login - Basic Tutorial

This section doesn't stop to explain the commands and syntaxes, they are dealt with earlier in this guide

After logging in with owner/owner for the first time, there are some things you'll do to fully move in and make it yours.  First and most importantly let's reset the password so anyone who wants to can't just login and do whatever they want in your MUSH.:

    !password owner=frogger

In this case, I set my password to 'frogger', but you should obviously use your own password there instead.  In a moment we're going to be doing things in the Master Room, rather than the new character starting room, so let's go there now:

    @tel 0

Next lets set the name of your game, and then reindex the game's HTML files so that it applies in your browser:

    @eval sts name "My Cool Game Name"
    @server/reindex    -or-    @srv/ri

If you refresh the browser you'll now see your name is the title of the tab.  Cool!  Using that "@eval sts..." syntax you can now set any of the game's settings you might want to change from defaults.  I like my characters to have a Master Character, that works similarly to the Master Room.  This makes for a very convenient place to put custom command handlers you want all of your players to have without cluttering up the Master Room.  There's nothing wrong with doing it the other way, this is strictly optional.  But if you want to try it out, here's how you'd do it as an example of what I meant earlier:

    @create/c Master Character (MC)
    @eval sts masterpc {find MC}

    






## Admin/Mod/Wizard Guide

### Incantation Before Mucking with the Server or File System

@srv/backup and record the number
@srv/shutdown

Do your business

Re-run the server

If your business included resetting the main game directory, run @srv/restore #
Where the # is obviously the number you recorded from the backup command

## Zelazny

### PDLs (Pipe-Delimited Lists)

Strings that start with a | can be treated in certain contexts as lists.  the list-* commands are the most obvious example, but this concept appears throughout zmush.

### Contexts, Functions and Executors

Function calls have a special permission step where the object that the function is on is checked for permission to do the thing if the user can't.

## Appendix:  Tutorial-level Snippers

    @attr here.outside=sunny
    It's {single {v outside} normal} here.

    @flag here=handler
    @attr here.$echo *=emit this %1
    @attr here.$1 *:*=do {emit here %2} {emit here %1}

This is for the master room (using %a emits to the right person, not to the room itself):

    @flag here=handler
    @attr here.$test=emit %a "The test worked"

    @cr List Demo Object (LDO)
    @flag LDO=Handler
    @attr LDO.$add *=set list {add {v list} %1}
    add 1
    add 2
    add I was here
    @ex LDO

    ;You can also rewrite the bit from LDO above from:
    @attr LDO.$add *=set list {add {v list} %1}
    ;to:
    @attr LDO.$add *=set list {add LDO.list %1}

    @eval map "1|2|3" {concat "Hi " %i}


### Fun with functions and auto-Vs

    @cr Function Object (FNO)
    @attr FNO.>fn(x y)=emit %a `The coords are ([x], [y])`
    @eval FNO.fn 10 15

### Building +home and +bind on the master character:

    @create/player Master Character
    @flag Master Character=Handler
    @flag Master Character=Teleporter

    @attr Master Character.home=no
    @attr/lock Master Character.home=id:2
    @attr/lock Master Character.home=template

    @attr Master Character.$+home = [loc {v %a home}] {?? loc {do {move %a loc} {emit loc {concat %an " comes home"}}} {emit %a "You don't have a home set!"}}
    
    ;or, formatted nicely:
    
    [loc {v %a home}] 
    {if loc 
        {do 
            {move %a loc} 
            {emit loc {concat %an " comes home"}}} 
        
        {emit %a "You don't have a home set!"}}

    @attr Master Character.bindlist=|3
    @attr Master Character.$+bind=if {?contains 3.bindList %l} {do {set %a home %l} {emit %a "You set your home to this location."} } {emit %a "You can't bind here!"}

    ;or, formatted:
    if {?contains 2.bindList %l} 
        {do 
            {set %a home %l} 
            {emit %a "You set your home to this location."}} 
        {emit %a "You can't bind here!"}

    ;If you want to have areas that +home can't be used in, this snippet uses the S1 system flag to check this (setting S1 on a room makes it impossible to +home from there)

    [loc {v %a home}] 
    {?flag %l S1 
        {emit %a "You can't teleport from here!"} 
        {?? loc 
            {do 
                {move %a loc} 
                {emit loc {concat %an " comes home"}}}
            {emit %a "You don't have a home set!"}}}


## Appendix:  Common Snippets and Libraries



