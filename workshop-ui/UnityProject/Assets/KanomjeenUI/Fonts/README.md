# Kanomjeen UI Font Slot

Place the legally obtained Thai-capable Unity font asset here with the exact filename:

`KanomjeenThai.ttf`

The Editor builder will prefer:

`Assets/KanomjeenUI/Fonts/KanomjeenThai.ttf`

If it is missing, the prefab still builds with a Unity fallback font, but Workshop release must fail QA until Thai glyphs have been tested on a real client.

Recommended policy:
- use a font with clear redistribution rights (for example an OFL-licensed Thai family obtained from its official source)
- keep the font license notice in the Workshop/source repository as required by that font's license
- do not rename the file after prefab generation unless you rebuild the prefab

The release project now bundles **Noto Sans Thai** from the official Google Fonts
repository as `KanomjeenThai.ttf`. It is distributed under the SIL Open Font
License 1.1; the required license text is stored beside it as `OFL.txt`.

Source: https://github.com/google/fonts/tree/main/ofl/notosansthai
