# Localization

Every text the game shows, and every message the bot posts in chat, lives in
`Assets/Scripts/Localization.cs`, in English. Other languages are json files in
`Assets/StreamingAssets/Languages`; in a build that is `Ravenfall_Data/StreamingAssets/Languages`,
next to the game. `Assets/Scripts/Shinobytes/Languages.cs` loads them.

The streamer picks the language under Settings > Interface > Language. It is saved in the game
settings as `language`, and applies to the interface and to the bot's messages in their chat, since
one game posts to one channel.

## Translating

1. Start from `_template.json` in the Languages folder. It holds every text with its English. Make a
   fresh one with **Ravenfall > Localization > Export template** in the editor.
2. Copy it to a file named after the language code, such as `de.json`, and set `code` and `name`
   (the language's name as its speakers write it: Deutsch, Español, Svenska).
3. Translate the values. Keep the keys.
4. **Keep every placeholder.** `{player}`, `{itemName}`, `{0}` and the rest are filled in by the
   game. They can move within a sentence, but a translation that drops or renames one is ignored,
   with a warning in the log, and that text stays English.
5. **Lists keep their number of entries.** Menus index them, so a list of the wrong length stays
   English.
6. **Chat commands stay as they are:** `!join`, `!train all`, `!raid` and so on are what players
   type, whatever the language. So do Twitch emotes (`FeelsBadMan`, `Kappa`) and island names.
7. Anything left out stays English, so a language can be added a page at a time.

Test without a build: put the file in `Assets/StreamingAssets/Languages`, run
**Ravenfall > Localization > Reload language files**, and pick the language in the settings.

Chat messages begin with routing tags for the bot, such as `%[category:system]%`. The template leaves
them out and the game puts the English ones back when it loads a language, so translators never see
them.

## For developers

- New text goes in `Localization.cs` as a `public static string` (not `readonly`: `Languages`
  replaces the fields when a language is chosen). Use named placeholders for chat messages and
  `{0}` style for interface text, as the surrounding strings do.
- Read the field when you draw, not once at startup, or the text will not follow a change of
  language. Views that draw their labels once can listen to `Languages.Changed`; the settings
  window, the game menu and the shortcut menu do. Other windows pick up a new language when the
  game reloads.
- After adding texts, export the template again so translators see them.

## What is not translated yet

- About 220 chat replies still written straight into the code rather than in `Localization.cs`.
- A few labels typed into the UXML of the code of conduct and update screens, and the uGUI screens
  not yet moved to UI Toolkit (login, player search).
- Names that come from the server: items, skills, islands.
- Fonts: the game font covers Western European languages, Russian, Greek and Japanese. Polish,
  Czech and Turkish letters, Chinese, Korean, Thai and Arabic need fallback fonts, and right to left
  languages need more than that.
