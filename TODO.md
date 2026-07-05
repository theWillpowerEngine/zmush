* Exits
  * Lock and transport messages

* WHO (use SpecialOutput)

* Implement .z file load on startup
  * A way to add formatters

* A setting to turn off object auto-persistence and a timed save and @save command
    * Consider just doing this flat out for very chatty objects like PCs

* externalize the HTML rendering to settings (stuff like the nbsps and brs as well as CSS)

* Setting and implementation for multi character per user (will need UI too)



### Changelogs

#### 0.0.1 -> 0.0.2 

* Added 'sts' and 'stg' keywords, and the concept of special zelazny keywords (sts) which can only be used in particular situations
* Added 'emit' and 'do' keywords
* Added custom command handlers, and 'handler' flag