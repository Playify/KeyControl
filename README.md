# KeyControl

Get control over your desktop windows, override the CapsLock key, fix spelling mistakes, use emojis from your keyboard and even calculate math expressions without ever leaving the program you are working with.


## Installation

To run this program, [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) is needed.


If you want to install the program, open the exe file, and in the tray menu, click `Install`.
Alternatively, there is also an install button inside the menu, that can be accessed via Win+CapsLock > KeyControl

If you don't want to install it, you can just run it as is. If you want, you can create a blank `config.json` file
alongside KeyControl.exe, if you want to make it fully portable. Sadly the .NET 6 requirement is still there,
but if you really want to, you can compile a standalone exe yourself by enabling SelfContained in csproj, but that makes the file ~130MB big.

### Updates

There is no auto-update feature. Download the new exe, run it, and in the tray menu press the `Install` button again.


## Features

The settings menu can be accessed either via the tray menu, or by pressing Win+CapsLock.

### Window Control
- Always on top (`Ctrl+Win+T`)
- Borderless (`Ctrl+Win+B`)
- Clickthrough (`Ctrl+Win+H`)
- Fullscreen (Borderless) (`Ctrl+Win+F`)
- Hide/unhide windows (`Ctrl+Win+M / Ctrl+Win+N`)
- Transparency toggle & adjustments (`Ctrl+Win+MiddleClick / Ctrl+Win+Scroll`)

### Advanced Window moving
- Move to current screen & maximize (`F1`)
- Dragging window (`Ctrl+Win+Left Mouse`)
- Resizing window (`Ctrl+Win+Right Mouse`)

### Hotkeys
- Get color under cursor (`Ctrl+Win+C`)
- Google selected text (`Ctrl+Win+G`)
- Keep key(s) down (`Ctrl+Win+K`)
- Kill focused program (`Ctrl+Win+F4`)
- Break out of games (`Ctrl+Win+Pause`)
- Kill KeyControl (`Ctrl+Win+Esc`)

### CapsLock Remapping
- Custom actions (including key combos, raw text, etc.)
- Toggle normal CapsLock (`Ctrl+CapsLock` or `Shift+CapsLock`)
- Momentary override, so text can be "pasted" into remote desktop tools, that don't have good copy-paste support

### Gaming Features
- Use your controller as a mouse
- Crosshair overlay (customizable color, size, thickness, dot)
- WASD rotation challenge

### Inline Tools
- Quick calculator (`NumLock`, or `@=EXPRESSION`)
- Flip text (`@flip text`)
- Unicode conversion (`@uCODE`)
- Number conversion (`@hdNUMBER` => 255), h=hex,b=binary,d=decimal

### Custom Hotstrings
- Fully configurable text expansions & replacements that apply directly while typing
- spelling: e.g. laod => load
- emojis: e.g. hzz => ❤ with multi-emoji support, hzzz => ❤❤ 
- unicode helpers: e.g. @full => █, @diameter => ⌀
- shorthands: e.g. @@ => myemail@gmail.com (not included by default, but can be easily configured)

### Special characters
- Similar to the existing `AltGr+Q` becoming `@`, a lot more special chars got added.
- `AltGr+P` => π (Pi)
- `AltGr+O` => Ω (Ohm)
- `AltGr+A` => α (Alpha)
- `AltGr+B` => β (Beta)
- `AltGr+T` => τ (Tau)
- `AltGr+L` => λ (Lambda)
- `AltGr+Space` => Zero-Width-Space
- `AltGr+Y` => | (Normal vertical bar, useful for laptop keyboard without `<>|` key)