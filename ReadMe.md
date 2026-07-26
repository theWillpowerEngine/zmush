A web-based re-interpretation of the classic MUSH/MUX.  Website coming soon at https://zmu.sh/

"Complete" Reference Guide:  https://docs.google.com/document/d/1J9dWANA0xQfWYWvj0RUqwac7M3QZLCJe5KIG6l-wQi0

### Changelogs

#### 0.0.4

* Removed the list- prefix for PDL keywords it was more annoying that helpful
* Added 'if' alias for ??, since that's one way to use that predicate
* The ZObject executing a function/command handler is now saved in a special hidden register and checked for permissions/access
* Added template lock for attributes
* Key system flags will no longer inherit
* Added a code editor with (very) basic IDE functionality so it's not torture to write Zelazny anymore
* Added flags:  Sealed, Teleporter, SetAndGet
* Made special output a visibly-different section of the UI and added "copy to clipboard" on click
* Added commands:  @chown, @parent, @attr/lock, @attr/listlocks, @attr/unlock, @decompile, !who
* Added keywords:  find, roll, roll-pool, move, +, -, *, /, item-at, filter, map, join, eval
* Added predicates:  ?flag, ?and, ?or, ?oid, 

#### 0.0.3 

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