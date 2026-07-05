* WHO (use SpecialOutput)

* A setting to turn off object auto-persistence and a timed save and @save command
    * Consider just doing this flat out for very chatty objects like PCs

* A way to add formatters in zelazny
* externalize the HTML rendering to settings (stuff like the nbsps and brs as well as CSS)

* Attribute locks/flags of some kind (for special and hidden attributes)

* Setting and implementation for multi character per user (will need UI too)

### Changelogs

#### 0.0.2 -> 0.0.3 

* Added special registers %a (actor id) and %an (actor name)
* Added custom command handlers, and 'handler' flag
* Added @eval command
* Added 'sts' and 'stg' keywords, and the concept of special zelazny keywords (sts) which can only be used in particular situations
* Added keywords:  do, emit, log, match
* Added singleton predicates:  ??
* Added comparison predicates:  ?=, ?!