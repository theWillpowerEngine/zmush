* autolet
* Add variables to set (may need a syntax to disambiguate or a new keyword?)

* We need a (probably attribute-based) metaphor for functions (maybe attribute and zelazny based so that there can be globals)

* Attribute locks/flags of some kind (for special and hidden attributes)
* Template attributes (use for character creation PoC)

* Consider adding "layer" tags like ooc where people can add stuff that folks can flag to subscribe or unsubscribe to
  * Config driven?

* @decompile

* WHO (use SpecialOutput)

* A way to add formatters in zelazny
* externalize the HTML rendering to settings (stuff like the nbsps and brs as well as CSS)

* Setting and implementation for multi character per user (will need UI too)

* Consider a queueing system for commands to avoid speed-spamming the endpoint
  * Take this chance to actually hook up quota too?

*Session Timeout

### In-Game Projects

* Finish Character Creation/IC PoC
* Public chat channels (make it a library and use as a PoC for zelazny libraries)


### Changelogs

#### 0.0.2 -> 0.0.3 

* Changed ZObjects to (by default) dirty themselves and save on a timer.  Can still be switched to saving in real time with AutoSaveMins setting.
* Added check locks for exits, and refined allow/deny/pc locks to run on space-delimited lists and handle both #N and N formats
* Added reader auto-let macro: [name val...] action
* Added custom command handlers, and 'handler' flag
* Added special registers like %a (actor id), %an (actor name) and the various pronouns (%as, %ao, %ap) along with numbered registers %1-%9
* Added commands:  @eval, @user (and various subcommands for user management), !exit, @server (backup, restore and shutdown)
* Added 'sts' and 'stg' keywords, and the concept of special zelazny keywords (sts) which can only be used in particular situations
* Added keywords:  concat, do, emit, force, let, list-add, list-remove, list-remove-all, list-index, log, match, set, string
* Added singleton predicates:  ??
* Added comparison predicates:  ?=, ?!, ?contains
* Added reader shortcut for 'string' keyword (ticks as string delimiters)