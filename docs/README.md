# WoLua scripting
_How do I do things?_

Please note that this documentation assumes familiarity with [the lua scripting language](https://www.lua.org/). If you don't at least know the basics of lua's syntax and some simple programming concepts like variables and conditionals, you should start there first.

## The basics
Within your base script folder (set in the `/wolua` settings section), you must create subfolders that contain the lua script files. In order to execute your custom commands, you need to provide the folder name followed by any arguments to the script command, so any spaces in the folder name will be stripped out. For example, a script folder named `My Command` will be given the command name `MyCommand` instead. Don't worry about capitalisation too much; when running a command, the name is checked as written, then in all lowercase, then in all caps, so you could still call `mycommand` or even `MYCOMMAND` and it would work fine.

Within a script's particular (sub)folder, only one file is actually required: `command.lua` is the only file that will be loaded directly. Any other files will be treated as function libraries and only loaded if the `command.lua` file calls `require` or `loadfile` on them. This system was chosen in order to avoid function name conflicts in various command and library files, and to allow easier sharing of script commands via git repositories. For this reason, each `command.lua` file can _only_ load library files in its own directory.

## Script lifecycle
In WoLua, scripts are loaded _and executed_ immediately when the plugin scans for them. This happens in three cases:

1. When WoLua starts up, each time you launch the game
2. When you use the `/wolua reload` command
3. When you change the base folder path in the settings section of the main window, accessed by `/wolua`

Each script is held in a separate instance of WoLua's lua engine, [MoonSharp](http://www.moonsharp.org/). This means that separate commands cannot touch or even see each other. In fact, commands don't even know how many other commands are loaded by WoLua, never mind what they're called. These script instances are held onto until one of two things happens: either WoLua unloads (for instance, when you quit the game or update the plugin), or a rescan is triggered as described above.

Since scripts are loaded and executed automatically, they can perform startup initialisation by simply doing whatever they need. Script APIs are fully loaded and [persistent storage][script storage] is automatically loaded from disk (if found) before the script is executed, so everything is set and ready to go immediately. However, there is no way for a script to perform any actions automatically when unloading; allowing that would both be unnecessary and present a risk of delaying unloads.

When a script is invoked by the user via `/wolua call <script>`, the script's _registered callback function_ is run. Obviously, this means that all scripts must _have_ such a function. As a result, any script that fails to register one during its initialisation phase will be noted, and the user will be given an error for each such script. Furthermore, they will not be usable (again, obviously) and attempting to do so will simply print a chat error.

## Script callback functions
To register a callback function, a lua function must be passed to the `Script` API object. You can find more details about the `Script` API on the [Script API page][script api], but this is the most basic usage. The callback function _may_ take one argument, or it may take zero if it doesn't need any user input. When the script is called via `/wolua call <script>`, this function will be invoked, and will be passed any additional text provided after the script name, or an empty string if nothing else was written.

> :warning: If there's more than one space between the script name and any additional text, only _one_ space will be snipped off so as to preserve any intentional formatting. Code accordingly.

## Accessible APIs
Since there's a fair amount of information exposed, not to mention the functions to perform various actions, there are several different APIs that can be accessed. Each has their own page, providing more details.

- The [Script API][script api] has functions that don't relate to the game itself
- The [Debug API][debug api] contains everything related to debugging scripts
- The [Keys API][keys api] contains a handful of properties to test if modifier keys (control, alt, shift) are down
- The [persistent storage table][script storage] isn't technically an API, but it has its own documentation
- The [action queueing API][action queueing] allows emulation (and better) of vanilla macros' `<wait>` functionality
- The [Game API][game api] is the overall access point for everything relating to the game itself
- The [Dalamud API][dalamud api] provides information about Dalamud, such as the current version and loaded plugins
- The [Player API][player api] has details about the current player
- The [Chocobo API][chocobo api] has details about the current player's _combat_ chocobo
- The [Party API][party api] has details about the current player's party and party members
- The [Toast API][toast api] contains a handful of functions for creating the "toast message" popups, like quest objective completion or moving to a new part of the map

## Querying values

Since a lot of game values are stored as numeric IDs and translated into the client language, trying to check conditions against text strings can be tricky at best. For this reason, WoLua exposes the internal IDs of many things for simpler checks. Unfortunately, this then raises a new problem: how do you _get_ the ID of the thing you want to check for? You could write a script that just prints it into your chat, but WoLua also includes a command to query (at least some of) those IDs for you: `/wolua query`.

In order to query a value, you have to specify _what_ value you're interested in, as an argument to the `query` subcommand. Each one will print relevant information to your chat, or - if for some reason, you aren't logged into a character - an error about the information not being available. Please note that all queries require you to be logged in and _not_ in a loading screen, since the relevant data isn't available when you don't technically "exist" in the game.

- `id` will print your _universally unique_ character ID, which may be useful for storing per-character settings in scripts
- `class` and `job` will both print your class/job ID, name, and abbreviation, along with your current level as known to scripts (via `Game.Player.Level`)
- `mount` will print the ID and name (with appropriate `a`/`an` article) of your current mount, if you're mounted
- `zone` will print the ID of your current map zone; be aware that different wards in housing districts and different estates of the same size each share the _same_ zone ID
- `weather` will print the ID, the Title Case name, and the description for whatever the current zone's active weather is

If there are other values you would like queryable, please [open an issue][issue tracker] and make a request.



[chocobo api]: <chocobo.md>
[dalamud api]: <dalamud.md>
[debug api]: <debug.md>
[game api]: <game.md>
[keys api]: <keys.md>
[party api]: <party.md>
[player api]: <player.md>
[action queueing]: <queue.md>
[script api]: <script.md>
[script storage]: <storage.md>
[toast api]: <toast.md>

[issue tracker]: <https://github.com/VariableVixen/WoLua/issues?q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc>
