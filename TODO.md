* Finish let scopes

* @decompile

* WHO (use SpecialOutput)

* A way to add formatters in zelazny
* externalize the HTML rendering to settings (stuff like the nbsps and brs as well as CSS)

* Attribute locks/flags of some kind (for special and hidden attributes)

* Setting and implementation for multi character per user (will need UI too)

* Consider a queueing system for commands to avoid speed-spamming the endpoint
  * Take this chance to actually hook up quota too?

*Session Timeout

### Changelogs

#### 0.0.2 -> 0.0.3 

* Changed ZObjects to (by default) dirty themselves and save on a timer.  Can still be switched to saving in real time with AutoSaveMins setting.
* Added check locks for exits, and refined allow/deny/pc locks to run on space-delimited lists and handle both #N and N formats
* Added custom command handlers, and 'handler' flag
* Added special registers like %a (actor id), %an (actor name) and the various pronouns (%as, %ao, %ap) along with numbered registers %1-%9
* Added commands:  @eval, @user (and various subcommands for user management), !exit, @server (backup, restore and shutdown)
* Added 'sts' and 'stg' keywords, and the concept of special zelazny keywords (sts) which can only be used in particular situations
* Added keywords:  concat, do, emit, force, list-add, list-remove, list-remove-all, list-index, log, match, set, string
* Added singleton predicates:  ??
* Added comparison predicates:  ?=, ?!, ?contains
* Added reader shortcut for 'string' keyword (ticks as string delimiters)