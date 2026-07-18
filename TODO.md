* Add character name to the document.title for ease of multi-boxing

* Find a way to limit who can call functions so that we don't just expose admin functionality (A special flag that requires roles to set?  A series of locks?)

* Special permissions (codify it as a system):
  * Can ignore sealed
  * ...

* Attribute visibility locks (dark) ??

* More PDL methods:  filter, ...

* Consider adding "layer" tags like ooc where people can add stuff that folks can flag to subscribe or unsubscribe to
  * Config driven?

* A way to add formatters in zelazny
* externalize the HTML rendering to settings (stuff like the nbsps and brs as well as CSS)

* Setting and implementation for multi character per user (will need UI too)

* Consider a queueing system for commands to avoid speed-spamming the endpoint
  * Take this chance to actually hook up quota too?
  * Slow and Fast modes (settings based?)

* Session Timeout

* This is aids but at some point we need an "IDE" for writing Zelazny even I resort to using Kate or VSCode for long shit

### In-Game Projects

* Finish Character Creation/IC PoC
  
  @attr cg.$+attrs=?? {v %a Internal} {emit %a "You've already set your attributes.  If you beg an admin they might reset them, but for now you're stuck with them"} {do {} {} {}}


* Public chat channels (make it a library and use as a PoC for zelazny libraries)


### Changelogs

#### 0.0.3 -> 0.0.4

* Removed the list- prefix for PDL keywords it was more annoying that helpful
* Added 'if' alias for ??, since that's one way to use that predicate
* The ZObject executing a function/command handler is now saved in a special hidden register and checked for permissions/access
* Added template lock for attributes
* Key system flags will no longer inherit
* Added flags:  Sealed, Teleporter
* Made special output a visibly-different section of the UI and added "copy to clipboard" on click
* Added commands:  @chown, @parent, @attr/lock, @attr/listlocks, @attr/unlock, @decompile, !who
* Added keywords:  roll, roll-pool, move

#### 0.0.2 -> 0.0.3 

* Changed ZObjects to (by default) dirty themselves and save on a timer.  Can still be switched to saving in real time with AutoSaveMins setting.
* Added check locks for exits, and refined allow/deny/pc locks to run on space-delimited lists and handle both #N and N formats
* A name by itself is now a valid zelazny program
* Added reader auto-let macro: [name val...] action
* Added custom command handlers, and 'handler' flag
* Added special registers like %a (actor id), %an (actor name) and the various pronouns (%as, %ao, %ap) along with numbered registers %1-%9
* Added commands:  @eval, @user (and various subcommands for user management), !exit, @server (backup, restore and shutdown)
* Added 'sts' and 'stg' keywords, and the concept of special zelazny keywords (sts) which can only be used in particular situations
* Added keywords:  concat, do, emit, force, let, list-add, list-remove, list-remove-all, list-index, log, match, set, setv, string
* Added singleton predicates:  ??
* Added comparison predicates:  ?=, ?!, ?contains
* Added reader shortcut for 'string' keyword (ticks as string delimiters)