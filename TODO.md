* ?? and ?! preds

* Exits
  * Lock and transport messages

* WHO (use SpecialOutput)

* Implement .z file load on startup
  * A way to add formatters

* A setting to turn off object auto-persistence and a timed save and @save command
    * Consider just doing this flat out for very chatty objects like PCs

* externalize the HTML rendering to settings (stuff like the nbsps and brs as well as CSS)
* Attribute locks/flags of some kind (for special and hidden attributes)

* Setting and implementation for multi character per user (will need UI too)

* Add other registers (actor?  location?  The "master room emits in itself" problem)


### Changelogs

#### 0.0.1 -> 0.0.2 

* Added custom command handlers, and 'handler' flaG
* Added 'sts' and 'stg' keywords, and the concept of special zelazny keywords (sts) which can only be used in particular situations
* Added keywords:  do, emit, match
* Added singleton predicates:  ?any
* Added comparison predicates:  ??, ?!