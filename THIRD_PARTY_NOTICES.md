# Third-Party / Reference Notes

## Implementation policy
Kanomjeen Suite is implemented as project-specific source rather than by copying and renaming third-party plugin source.

Behavioral references used during design included common RocketMod/Unturned plugin patterns for TPA, Homes, Kits, respawn protection, building limits, airdrops, stats, moderation and vehicle management. Those references were used to identify requirements and edge cases, not as permission to copy source.

## Toolchain/runtime references
The projects reference RocketModFix/LDM/Unturned/Unity compile-redist packages configured in `Directory.Build.props`. Their own licenses and distribution terms remain separate from Kanomjeen project source.

## Workshop/UI
The repository does not contain Unturned's `Project.unitypackage`; obtain it from the installed game's `Extras/Sources` directory as documented by Smartly Dressed Games.

The Workshop Unity project bundles Noto Sans Thai from the official Google Fonts repository as `KanomjeenThai.ttf`. Noto Sans Thai is distributed under the SIL Open Font License 1.1. The license is preserved at `workshop-ui/UnityProject/Assets/KanomjeenUI/Fonts/OFL.txt`, and the upstream source is https://github.com/google/fonts/tree/main/ofl/notosansthai.

## License gate for future borrowed code
Before copying any code from another plugin:
1. locate the exact repository/package license;
2. determine whether modification and redistribution are permitted;
3. preserve attribution/source/license text when required;
4. if source rights are unclear, use behavior only as a requirement and implement independently;
5. do not mix GPL source into a differently licensed/proprietary Kanomjeen release without intentionally accepting the GPL obligations.

## Kanomjeen project license
No public source license is selected by this repository. Choose one before publishing the project source. Steam Workshop distribution of built UI content is a separate release decision from publishing C# source.
