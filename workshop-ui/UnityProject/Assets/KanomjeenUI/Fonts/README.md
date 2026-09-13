# Kanomjeen UI Fonts

The Workshop UI ships **Kanit**, a Thai + Latin family, in three weights:

| File | Weight | Used for |
| --- | --- | --- |
| `Kanit-Regular.ttf` | 400 | body text, help lines, secondary values |
| `Kanit-SemiBold.ttf` | 600 | row titles, labels, small caps-style headings |
| `Kanit-Bold.ttf` | 700 | screen titles, buttons, hero numbers |

`KanomjeenUiBuilder` loads these by exact path. All three are referenced by the prefab, so all three
are embedded in the exported master bundle (`dynamic=True`, `fontNames=[Kanit]`).

## Why Kanit

`Supernovea Itemshop V2` and `Supernovea RankQuest V2` — the two commercial Effects this UI was
benchmarked against — both embed Kanit as their body font (measured from their shipped bundles; see
`AGENTS.md` §13.6). Kanit covers Thai and Latin in the same file, which is why their bilingual UI keeps
one consistent voice. Oswald/Anton, which those Effects also use, are Latin-only and only exist as
variable fonts in the Google Fonts repository, so Kanit Bold is used for display text here instead.

## Source and license

- Upstream: <https://github.com/google/fonts/tree/main/ofl/kanit>
- License: **SIL Open Font License 1.1** — `OFL.txt` sits beside the fonts and must stay in the
  repository and in any redistributed package.

Verified upstream git blob hashes (download and compare with `git hash-object`):

```
e9bc0a2f5d0d1ad0df1fa20c44e381834c128c58  Kanit-Regular.ttf     173148 bytes
0c79ade5d4d64cd0a0408e34a9b1219f042ea9c5  Kanit-SemiBold.ttf    174796 bytes
fc9110692bd709c4a940156c90b900093ab47c65  Kanit-Bold.ttf        176136 bytes
1fd9f8e65d07e0e5fcecba4386b28910ebffa456  OFL.txt                 4383 bytes
```

Download command:

```bash
for f in Kanit-Regular.ttf Kanit-SemiBold.ttf Kanit-Bold.ttf OFL.txt; do
  curl -L -o "$f" "https://raw.githubusercontent.com/google/fonts/main/ofl/kanit/$f"
done
```

## Policy

- Use a font with clear redistribution rights (OFL or equivalent) obtained from its official source.
- Keep the license notice next to the files; it is required by the license.
- Do not rename a font after prefab generation unless you rebuild the prefab — the builder resolves
  fonts by path, so a rename without a rebuild leaves the bundle on the fallback font.
- Thai glyphs must still be verified on a real client before a Workshop release (the bundle can embed
  a font that the client fails to render).
