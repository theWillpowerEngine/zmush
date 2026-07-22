"Complete" Reference Guide:  https://docs.google.com/document/d/1J9dWANA0xQfWYWvj0RUqwac7M3QZLCJe5KIG6l-wQi0


## Settings

    Name                Type        Action
    AutoLinkExits       bool        If true, exits will automatically be turned into Action Links, allowing them to be clicked on
    AutoSaveMinutes     int         Number of minutes between automatic ZObject save.  If 0 or less, ZObjects will save every time, which could impact disk life and performance under heavy use
    BreakOnException    bool        If true, the server will crash if an exception occurs.  You should basically never set this unless working on the C# code
    EditorPerms         string      If not empty, a permission required (besides admin) to launch to inline editor.  Requires index regeneration
    LogQuotaExceeds     bool        If true, any time a Zelazny evaluation is stopped because of quota it will log some useful tracing information
    MasterItem          int         ID of the Master Item, which is the default parent of every newly-created Item (default -1)
    MasterPC            int         ID of the Master PC, which is the default parent of every newly-created PC (default -1)
    MasterRoom          int         ID of the Master Room, which is the default parent of every newly-created room (default #1)
    Name                string      The name of the game (will effect the title when the index is next regenerated)
    ShowHTTP            bool        If true, will show all HTTP requests in the log (VERY spammy, but can be useful for debugging)
    StartRoom           int         ID of the room new PCs are created in


## Commands

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

    @examine (@ex) <obj>
    @decompile (@dc) <obj>

    @parent <obj>=<id>

    @password <user name>=<new password>
    
    @user <user name>=<pwd>
    @user/roles <user name>
    @user/enrole <username>=<role>
    @user/unrole <username>=<role>

    @lock <obj>=<lock>[:<lock argument>]
    @lock/list (@lock/l) <obj>
    @unlock <obj>=<lock>[:<lock argument>]

    @flag <obj>=<flag>
    @flag <obj>=!<flag>

    @attr <object>.<attribute>=<value>
    @attr <object>.<attribute>              //Shorthand for removing an attribute, you can also @attr/clear or @attr/c
    
    @attr/val (@attr/v) <obj>.<attr>

    @attr/list (@attr/l) <obj>

    @attr/lock (@attr/l) <obj>.<attr>=<lock>[:<val>]        //Set attribute lock
    @attr/unlock (@attr/u) <obj>.<attr>=<lock>
    @attr/listlocks (@attr/ll) <obj>.<attr>

    @create (@cr) <name>
        @cr/item (@cr/i)
        @cr/character (@cr/c)
        @cr/room (@cr/r)
        @cr/exit (@cr/x, @cr/ex)

        @cr/user (@cr/u) <name>[:<pwd>]=#<character id>         If no password the user name is used

    @nuke <obj>
    @nuke/global (@nuke/g) <obj>    - Global nuke.  Be careful with partial names, you should generally only use ids, lol

    @dig <name>
    @dig/1 <name>
    @dig/t <name>
    @dig/1t <name>

    @chown <obj>=<owner>

    @tel <room>         Can be #id or name part
    @tel <character>=<room>

    @eval <zelazny code>

    @server (@srv) subcommands:
        shutdown (sd)
        backup (b)
        restore (r) #   - the first backup is 0
        reindex (ri) - Reload the web server files and reapply any settings
        import (im) - Import/Install a library

    !who

    !exit (!ex)
    !password <current password>=<new password>

    clear


## Zelazny Keywords

    <operator> <left> <right> - Operators:  +, -, *, /

    concat <val> <val>... - Combine into a single string

    do <val>... - Evaluate each value and return the last one

    emit <obj> <message> - Send a message.  Context sensitive (can send to a player, a room or even an item)
    emit <message> - Send message to actor.  In rare situations where there is no actor the context is used instead.  Will generally send to the player who initiated the action.

    find <obj> - returns "" if no object can be found, otherwise returns the OID

    force <obj> <command> - Force <object> to run command

    id <obj> - Return ZOID

    let (<name> <val>)* <action> - Create variables.  

    log <val> - Logs a message with the 'zelazny' tag in the zmush log

    match <check> (<compare> <val>)... [<val>] - For each pair, if the compare value matches the check value, evaluate and return that pair's value.  The optional final value is if nothing matches

    move <obj> <loc> - Moves ZObjects.  Requires ownership/permission, or the Teleporter flag

    roll <sides>
    roll <number> <sides>
    roll-pool <number> <sides> - Returns a PDL of the individual dice rolls

    set [<obj>] <attr> <val> - Set attribute value on obj, or the context

    setv <name> <val> - Set variable value.  

    single <val>... - Returns the first of the values that follow it that isn't empty.

    stg <setting> - Gets the Engine Setting specified (see "Settings, Roles and Permissions" earlier in this guide for more info on Engine Settings)

    string (str) <interpolated string> - Anything in [] will be evaluated as code.  For example:  @eval str "The attr is: [v here a]"

    val (v) <attr> - Get value of attribute from context
    val (v) <obj> <attr> - Get alue of attribute from object

### PDL / List Keywords

    add <string> <val> - Return a new stringified list adding <val>.  String can be either a string or a list itself
    
    index <list> <val> - Return index of item in list (1 based).  0 if not found

    map <list> <command> - Evaluate command for each item in list, returning the new list.  %i will be the iterative element

    remove <string> <index> - Remove item from stringified list
    remove-all <string> <val> - Remove all val from list.  Returns count, or 0 if none
    
### Special Keywords

These can only be evaluated in special contexts, or by particular users

    sts <setting> <val> - Sets the Engine Setting specified (see "Settings, Roles and Permissions" earlier in this guide for more info on Engine Settings)

### Predicates

There are two formats a predicate can appear in.  They are, singleton predicates:

    <pred> <val> [<true> [<false>]]

And dual predicates:

    <pred> <val1> <val2> [<true> [<false>]]

The singleton predicates are:

     Predicate       Description
    ?? (if)         if the value is truthy
    ?num            if the value is numeric
    ?oid            If the value is a valid object id

And the dual predicates are:

     Predicate       Description
    ?=              if the values are equal
    ?!              if the values are not equal

    ?contains       if the first value contains the second.  First value can be a list, or a string
    ?flag           if the object referenced by the first value has the flag in the second

### Reader Autos

Auto-Interpolation:

    emit here `The attr is: [v here a]`
    ;translates to:
    emit here {str "The attr is: [v here a]"}

Auto-Let:

    [x 1 y 2] `([x], [y])`
    ;translates to:
    let x 1 y 2 `([x], [y])`

Auto-V:

    12.attr
    here.attr

Function calls

    12.f 1 2
    me.fWithNoParms

### Built in Formatters

    [blah] - Action link (command = text)
    [blah:cmd] - Action link (text = blah, command = cmd)
    [<c> text] - Color text.  c = red, yellow, green, blue, purple

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

    %i    Iterative element for list keywords