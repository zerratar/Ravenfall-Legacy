// Run with: unity command eval_file --file tools/items/class-gear-materials.cs --timeout 60 (edit mode)
//
// The materials of the class gear (v0.9.7): one per tier of each line, named exactly as its ItemMaterial value, in
// Assets/World/ClassGear/Resources/ClassGear/<Name>.mat. ItemManager.GetMaterial loads them from there by name (they
// are not in the list on the scene's ItemManager, which ends at ElderAtlarus).
//
// Each one is a copy of the Rune tier material (the "Glowing Synty Gear" shader with its masks) with the colors of
// the table below and the glow off (the glow tints the whole piece). Running it again updates the materials in
// place, so their GUIDs stay; a name that is no longer in the table is left alone.
//
// What the shader's colors paint on the class gear parts:
//   cloth    _Color_Primary            the main cloth: robes, tabards, tunics, hoods, the feathered cap
//   trim     _Color_Secondary          cloth trim: hems, cuffs, bands, crosses
//   leather  _Color_Leather_Primary    belts, straps, gloves, boots, the jerkin
//   dark     _Color_Leather_Secondary  the darker leather: boot shafts, pouches, leather hoods
//   plate    _Color_Metal_Primary      plate, helms, and the shirt under a robe
//   metal    _Color_Metal_Secondary    trim metal: buckles, edges of plate, mantles, the half-plate breastplate
//   mail     _Color_Metal_Dark         mail, trousers, undersleeves, the wizard hat
// Feathers and a few props keep the texture's own color. No part of the class gear shows skin, but the skin color
// is kept neutral in case one ever does.
//
// Palette direction (docs: the class gear spec and docs/taming-gear-design.md): mage dark and cool, healer light, ranger greens, browns and autumn
// tones, melee reds, rusts and dark steel, tank iron and steel first and then heraldic colors. Every tier of a line
// is clearly another color, and the richness grows from tier 1 to tier 8.
var table = @"
# name        | cloth   | trim    | leather | dark    | plate   | metal   | mail    | metallic | smoothness
# ---- Mage: dark and cool ------------------------------------------------------------------------------------
Apprentice    | #5A6578 | #BDB49C | #6E4A2C | #3E4656 | #CFC9BB | #8C7446 | #394050 | 0        | 0.2
Adept         | #2A3F78 | #A9B8D2 | #5B4630 | #1D2C55 | #D5DAE4 | #9AA3B2 | #1F2A4A | 0        | 0.2
Conjurer      | #1C5A62 | #C98A52 | #5A3A26 | #123E44 | #D9D2C0 | #B9783F | #153A40 | 0        | 0.2
Sorcerer      | #4B2C86 | #CFC6EC | #4A3340 | #33205C | #E4DEF2 | #B6B2C8 | #2B1A50 | 0        | 0.2
Warlock       | #1D1B22 | #9B55E2 | #2B2430 | #121116 | #6D5A8C | #8A4FD0 | #0F0E14 | 0        | 0.25
Archmage      | #6B1D4E | #E4B748 | #4A2A2A | #4A1436 | #F0E6DC | #E0AE3C | #3A1030 | 0        | 0.25
Voidweaver    | #2C1060 | #4FD8D0 | #2A2444 | #1A0B3A | #B8C8F0 | #46C2C8 | #12082E | 0        | 0.3
Starcaller    | #0E1640 | #F4C84E | #6A5530 | #0A0F2C | #EEF2FF | #F6CC52 | #080C28 | 0        | 0.3
# ---- Healer: light ------------------------------------------------------------------------------------------
Acolyte       | #B5AC95 | #6F8562 | #7A5A3A | #4A3626 | #D8D1C0 | #8F7448 | #6C675B | 0        | 0.2
Mender        | #DCD2B2 | #B5673F | #8A5E38 | #553A26 | #EDE6D2 | #A8683E | #8A7F6A | 0        | 0.2
Cleric        | #BCD0E4 | #3F5F96 | #7A6650 | #4A3E34 | #E8EEF4 | #5878A8 | #7F8FA6 | 0        | 0.2
Priest        | #E9E6DC | #B98A2E | #7A5A3A | #4A3626 | #F2F0E8 | #B08430 | #B5B2A8 | 0        | 0.25
Hierophant    | #F3E2E0 | #B8475F | #A06A62 | #6A4244 | #FAF0EE | #C2566C | #C9AEB0 | 0        | 0.25
Saint         | #DDF0E4 | #2F9A6B | #8C8468 | #56523E | #F2F8F2 | #3AA878 | #A9C4B4 | 0        | 0.25
Seraph        | #F4EEDA | #2F8FE0 | #B8A27C | #7A6A50 | #FBF8EC | #3C9AE6 | #BFC6D6 | 0        | 0.3
Lightbringer  | #FFFFFF | #FFD03A | #E6C06A | #C09A48 | #FFFFFF | #FFD23F | #F2ECD8 | 0        | 0.3
# ---- Ranger: greens, browns and autumn tones ----------------------------------------------------------------
Scout         | #68733F | #B9A87F | #7C5432 | #4B3321 | #8E9196 | #A8863F | #4F4B40 | 0        | 0.2
Tracker       | #7C5C3A | #8A9550 | #5E4028 | #382618 | #9A8F7C | #8F7A4A | #3E3A30 | 0        | 0.2
Hunter        | #3C7A32 | #D2C08A | #6E4424 | #402814 | #A0A6A8 | #B89648 | #2A3A2E | 0        | 0.2
Stalker       | #2D3A38 | #5E9A90 | #33302C | #1C1A18 | #6A7478 | #5A8A84 | #1A2224 | 0        | 0.2
Pathfinder    | #B8622A | #EAD9B0 | #6A3A20 | #3E2212 | #B0A898 | #D09A48 | #4A3424 | 0        | 0.2
Warden        | #C09A2E | #2F5A34 | #5A3E22 | #332212 | #A8A48E | #6E8A44 | #3A4028 | 0        | 0.25
Windrunner    | #8FB89C | #F2F4EC | #8A8478 | #55524A | #DDE4E0 | #C8D0CC | #50645A | 0        | 0.25
Hawkeye       | #0B4A34 | #F0C63E | #4A2E20 | #2A1A14 | #D6DBE2 | #EBC650 | #14261E | 0        | 0.3
# ---- Melee: reds, rusts and dark steel ----------------------------------------------------------------------
Footman       | #7A4636 | #B8AA88 | #6B4A2E | #3E2C1E | #8A8D92 | #74787E | #4B4E54 | 0.1      | 0.25
Mercenary     | #B0883A | #4A3420 | #70482A | #40281A | #7E8288 | #7A6248 | #3E4148 | 0.1      | 0.3
Duelist       | #7E1F36 | #D5D9E0 | #4A2A2E | #2A181C | #C9CED6 | #B8BEC8 | #55585F | 0.15     | 0.35
Reaver        | #C2561F | #2A2A2E | #3A2A22 | #1E1614 | #55585F | #3C3F46 | #26282C | 0.15     | 0.35
Champion      | #C62A2A | #F2E8D0 | #6A3A2A | #3C2018 | #D0D4DA | #C9A04A | #4A4E56 | 0.2      | 0.4
Warlord       | #5E1A1C | #C48A48 | #3E2418 | #20120E | #7A5A38 | #B07A3C | #1E1C1E | 0.25     | 0.4
Bloodsworn    | #9A0F1E | #1A1A1E | #30181A | #180C0E | #34343A | #26262C | #121214 | 0.25     | 0.45
Godslayer     | #18161A | #D21F30 | #3A1418 | #1C0A0C | #2A2A30 | #EAB43C | #0E0E10 | 0.3      | 0.5
# ---- Tank: iron and steel, then heraldic colors -------------------------------------------------------------
Guard         | #5E5648 | #9A8A68 | #5C4130 | #3A2A20 | #6E7074 | #7A6A50 | #3E3F42 | 0.15     | 0.3
Sentinel      | #4E5A6C | #B5A98A | #5E4634 | #3A2C22 | #A3A9B0 | #6E7680 | #45484D | 0.2      | 0.35
Defender      | #2446A8 | #F0E4C0 | #5A4030 | #36281E | #C6CCD4 | #E0B040 | #3A4660 | 0.2      | 0.4
Bastion       | #2F6B3E | #D8D2BC | #50442E | #2E281C | #8E948C | #C0A054 | #3A4238 | 0.2      | 0.4
Juggernaut    | #5A1418 | #D9A93A | #2A2220 | #151110 | #2C2D32 | #DCAC3C | #17171A | 0.3      | 0.45
Templar       | #B8262A | #F4F1EA | #7A5A40 | #4A3626 | #E6E8EC | #C0302E | #6A6E78 | 0.25     | 0.45
Ironclad      | #4A2A7A | #C8D0DC | #3A3440 | #201C26 | #4A6488 | #C2CAD6 | #22304A | 0.3      | 0.5
Aegis         | #F2EFE6 | #F2C544 | #B89A62 | #7A6238 | #F4F6FA | #F6CA4A | #9AA2B4 | 0.3      | 0.55
# ---- Rogue: dark greys and blacks, low contrast, one muted accent ---------------------------------------------
Cutpurse      | #4A4A48 | #6E6A5E | #4A3A2C | #2A2622 | #5A5A58 | #6A6458 | #33322F | 0        | 0.2
Footpad       | #3E4A44 | #5E7062 | #3E3428 | #24221E | #505A54 | #5A6A5C | #2A302C | 0        | 0.2
Prowler       | #3A3F52 | #5A6890 | #3A3038 | #22202A | #4A5064 | #56648A | #262A38 | 0        | 0.2
Shadowblade   | #2E3A40 | #2F8A8A | #2E2A2A | #1A1C1E | #3A474E | #2E8080 | #1C2428 | 0        | 0.25
Nightstalker  | #35263A | #7A3F8A | #2E2228 | #1C161E | #43334A | #70407E | #201826 | 0        | 0.25
Assassin      | #2A2A2E | #9A1F2A | #2A2022 | #161416 | #3A3A40 | #8E222C | #18181C | 0        | 0.25
Wraithwalker  | #3C4648 | #9FC4BE | #34383A | #1E2224 | #55625F | #8FB0AA | #242A2C | 0        | 0.3
Nightfall     | #14141C | #C9A23A | #241C20 | #0E0E12 | #26263A | #C09A38 | #0C0C12 | 0        | 0.3
# ---- Barbarian: furs, bone and raw leather, a warpaint color on the cloth -------------------------------------
Brute         | #6E5A44 | #C9BFA6 | #6A4A30 | #3E2C1E | #BDB29A | #8A7450 | #3A3028 | 0        | 0.15
Raider        | #5E4A38 | #D2C8AE | #7A5232 | #40281A | #C8BEA4 | #9A4A30 | #33261E | 0        | 0.15
Marauder      | #4E4A40 | #C4BCA8 | #5A4634 | #2E2620 | #B4AE9C | #3F6A7A | #2A2826 | 0        | 0.15
Berserker     | #5A2E26 | #D8CDB4 | #6E3E28 | #3A2018 | #CFC4AA | #B8322A | #2E1C18 | 0        | 0.15
Ravager       | #3E3A36 | #B8B0A0 | #4A3E36 | #26221E | #A8A294 | #5E7A3A | #1E1C1A | 0        | 0.15
Warchief      | #6A4E2A | #E6D9B4 | #7A5A30 | #40301A | #E0D4B0 | #C8922E | #33281A | 0        | 0.2
Stormborn     | #4A5560 | #DCE4EA | #5A6068 | #2C3238 | #D0D8DE | #3E8FD0 | #22282E | 0        | 0.2
Worldbreaker  | #2A2422 | #F0E6CC | #4A2E22 | #1A1412 | #E8DEC4 | #F06A1E | #141010 | 0        | 0.2
# ---- Samurai: lacquer red and black first, then other lacquer colors with gold ----------------------------------
Ashigaru      | #7A2E28 | #2A2624 | #4A3428 | #2A201A | #5A5550 | #8A7450 | #33302E | 0.1      | 0.3
Ronin         | #3E3E44 | #8A8478 | #4A3E34 | #26221E | #6A6A70 | #7A7468 | #2A2A2E | 0.1      | 0.3
Bushi         | #B02A24 | #1E1C1C | #5A2A22 | #2A1614 | #2E2C2C | #C8A040 | #1E1C1C | 0.15     | 0.4
Hatamoto      | #1F3F8A | #E8E0CC | #2A3050 | #181C30 | #2A2E3A | #D0A840 | #1A1E2A | 0.15     | 0.4
Kensei        | #1C1C20 | #C8C8D0 | #2A2A30 | #141418 | #B8BCC4 | #D8D8E0 | #101014 | 0.2      | 0.45
Daimyo        | #2E6E3E | #E0B840 | #2A4030 | #16241A | #2A2E2A | #E4BC44 | #18201A | 0.2      | 0.45
Shogun        | #5A1F7A | #F0C84A | #3A2048 | #20122A | #26222C | #F2CA4E | #1A141E | 0.2      | 0.5
Tenno         | #F2EEE2 | #E8B840 | #8A2A22 | #4A1814 | #F6F2E8 | #F4CC50 | #A82A22 | 0.2      | 0.5
# ---- Paladin: silver and white plate, light blues and gold (lighter than the Tank's heraldic colors) -----------
Initiate      | #7E8A9A | #C8C4B8 | #6A5A48 | #40362C | #9AA0A8 | #868E98 | #50565E | 0.2      | 0.35
Crusader      | #DAD6C8 | #3E6FB0 | #6E5440 | #423226 | #C6CAD0 | #A8B0BA | #60666E | 0.2      | 0.4
Justicar      | #6FA8DC | #F4F0E4 | #6A5A4A | #3E342A | #D4D9E0 | #C4CAD2 | #56607A | 0.25     | 0.4
Vindicator    | #2A8A9A | #F0E8D0 | #5E5040 | #382E24 | #D8DDE2 | #D0B050 | #3E5A64 | 0.25     | 0.45
Protector     | #F0ECE0 | #58A0E0 | #8A7458 | #54452F | #E4E8EE | #62A8E6 | #7A8AA0 | 0.25     | 0.45
Exemplar      | #E8C24A | #FFFFFF | #8A6A3A | #54401F | #EEF0F4 | #F0F2F6 | #8A7A50 | 0.3      | 0.5
Lightsworn    | #B8D8F4 | #F2C84A | #9A8660 | #5E5238 | #F4F6FA | #F2C84A | #8AA0C0 | 0.3      | 0.5
Archon        | #2A50C8 | #FFD84A | #B89A5A | #6E5A30 | #FFFFFF | #FFD44A | #C8D0E4 | 0.3      | 0.55
# ---- Dark Knight: blackened steel with one grim accent, never gold ----------------------------------------------
Blackguard    | #3A3A3E | #6A6A70 | #2E2A28 | #18161A | #3E4046 | #5A5C64 | #222226 | 0.3      | 0.4
Dreadknight   | #4A1A1C | #8A2A2A | #2E2022 | #181214 | #2E2E34 | #8A2A2E | #1A1A1E | 0.3      | 0.4
Oathbreaker   | #2A3A2A | #5E7A30 | #262A22 | #141612 | #2C3030 | #6A8A34 | #181C18 | 0.3      | 0.45
Doomguard     | #3A2A1A | #A85A1A | #2A2220 | #161210 | #2A2A2E | #B8601C | #18161A | 0.3      | 0.45
Deathlord     | #2A2A30 | #B8C0C8 | #2A2A2E | #141418 | #24262C | #A8B0BA | #121216 | 0.35     | 0.5
Nightlord     | #2E1A4A | #7A44C0 | #241C2E | #120E18 | #26222E | #7A44C0 | #141018 | 0.35     | 0.5
Voidknight    | #101830 | #2E8AC8 | #1A1E2A | #0C0E16 | #1C2030 | #2E8AC8 | #0A0C14 | 0.35     | 0.55
Abyssal       | #1A0A0C | #D82828 | #2A1214 | #100608 | #141416 | #D82828 | #0A0A0C | 0.35     | 0.55
# ---- Witch Hunter: browns, greys and oxblood leather with brass ------------------------------------------------
Seeker        | #7A6A52 | #4A3E30 | #6A4A30 | #3A2A1E | #8A8478 | #9A8450 | #4A4640 | 0        | 0.2
Inquisitor    | #5A5A5E | #2E2E32 | #5A4030 | #30241C | #8E9094 | #A88A48 | #3A3A3E | 0        | 0.2
Purifier      | #6E2A26 | #2A1A18 | #4A2A22 | #2A1614 | #8A8680 | #B8964A | #33282A | 0        | 0.25
Exorcist      | #3A3E4A | #B8B4A8 | #4A3A30 | #261E1A | #9498A0 | #B49A58 | #24262E | 0        | 0.25
Witchfinder   | #4A3A26 | #C8A04A | #5A3E24 | #2E2014 | #9A9484 | #C8A04A | #2A241C | 0        | 0.25
Demonhunter   | #2A2A2E | #A82A2A | #3A2222 | #1C1214 | #7A7C82 | #C0A050 | #18181C | 0        | 0.3
Nightwatch    | #1E2C44 | #C0C8D4 | #2E2E38 | #16181E | #A0A8B4 | #C8CCD4 | #121824 | 0        | 0.3
Vanquisher    | #E4DED0 | #7A1E22 | #5A2A24 | #30161A | #D8D4C8 | #E0B84A | #3A3034 | 0        | 0.3
# ---- Monk: undyed cloth, ochre, saffron and earth, ending in white and gold -------------------------------------
Novice        | #A89878 | #7A6648 | #6A5038 | #5A4632 | #C8BEA8 | #8A7450 | #7A6E5A | 0        | 0.15
Disciple      | #C8A04A | #8A5A2A | #6E4E30 | #5E4028 | #D8CCB0 | #A07A40 | #7A5E3A | 0        | 0.15
Ascetic       | #9A5A2E | #E0C89A | #5E3E28 | #4A3020 | #D8C8A8 | #B08040 | #5A4030 | 0        | 0.15
Sage          | #E08A1E | #7A2E1A | #6A4028 | #5A3020 | #E8D8B8 | #C89030 | #8A3A1E | 0        | 0.2
Mystic        | #8A2E2A | #E8A83A | #5A2E24 | #48201A | #E4D4B4 | #D8A040 | #5E2420 | 0        | 0.2
Enlightened   | #E8C040 | #F4EEDC | #8A6A3A | #70562E | #F0E8D4 | #E0B840 | #B88A30 | 0        | 0.2
Transcendent  | #F0E8D8 | #E08A1E | #A8825A | #8A6844 | #F6F0E2 | #E8B040 | #C8BCA0 | 0        | 0.25
Celestial     | #FFFFFF | #F6C840 | #D8B878 | #B89858 | #FFFFFF | #F8CC48 | #E0A830 | 0        | 0.25
# ---- Beastmaster: hide, fur and bone in earthy browns and greens, the teal of the Beastmaster tree growing in ----
Fledgling     | #6E6A4A | #A89A72 | #6A4A2E | #3E2C1C | #BFB49A | #9A8058 | #4A4234 | 0        | 0.15
Trapper       | #56603A | #B8A878 | #5E3E24 | #36241A | #C4B898 | #8A6440 | #3A3424 | 0        | 0.15
Houndmaster   | #3E5A34 | #D0BC8A | #6A4426 | #3A2414 | #C8BC9C | #7A5636 | #2E3A28 | 0        | 0.15
Falconer      | #4E5E66 | #4FB8AE | #5E3E26 | #33220F | #CFC4A6 | #A0784A | #22302E | 0        | 0.2
Wildheart     | #3E6A2E | #3FD0C9 | #6E4A2C | #3A2616 | #D6CCAE | #6E5030 | #1E2C22 | 0        | 0.2
Packlord      | #3A4446 | #3FD0C9 | #4A3A2E | #261E18 | #DCD2B8 | #5A5450 | #1E2224 | 0        | 0.2
Primal        | #24584A | #E0B24A | #3E2A18 | #22160C | #E4DAC0 | #7A4A26 | #1A2A22 | 0        | 0.25
Wyrmkin       | #0E4A48 | #F0C64A | #4A2E1C | #24160E | #E8DEC6 | #2E7A70 | #0A2A28 | 0        | 0.25
";
// a run that writes only these (comma separated names; empty for every row): the other lines' materials stay as
// they are on disk
var only = "";
var folder = "Assets/World/ClassGear/Resources/ClassGear";
var skin = "#C99A7A";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var template = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Materials/Rune.mat");
if (!template) return "no template material (Assets/Materials/Rune.mat)";
if (template.shader == null || !template.shader.name.Contains("Glowing Synty Gear")) return "the template's shader is not Glowing Synty Gear: " + (template.shader ? template.shader.name : "none");

// the rows, checked against the enum before anything is written
var rows = new System.Collections.Generic.List<string[]>();
var problems = new System.Text.StringBuilder();
foreach (var raw in table.Split('\n'))
{
    var line = raw.Trim();
    if (line.Length == 0 || line[0] == '#') continue;
    var cells = line.Split('|');
    for (var i = 0; i < cells.Length; i++) cells[i] = cells[i].Trim();
    if (cells.Length != 10) { problems.Append(" bad row: " + line + ";"); continue; }
    if (!System.Enum.TryParse<RavenNest.Models.ItemMaterial>(cells[0], out var value) || value.ToString() != cells[0]) problems.Append(" not an ItemMaterial: " + cells[0] + ";");
    for (var i = 1; i <= 7; i++) if (!UnityEngine.ColorUtility.TryParseHtmlString(cells[i], out _)) problems.Append(" bad color in " + cells[0] + ": " + cells[i] + ";");
    rows.Add(cells);
}
var first = (int)RavenNest.Models.ItemMaterial.Apprentice;
var last = (int)RavenNest.Models.ItemMaterial.Wyrmkin;
for (var v = first; v <= last; v++)
{
    var name = ((RavenNest.Models.ItemMaterial)v).ToString();
    if (!rows.Exists(r => r[0] == name)) problems.Append(" no row for " + name + ";");
}
if (problems.Length > 0) return "nothing written:" + problems;
if (only.Trim().Length > 0)
{
    var wanted = new System.Collections.Generic.HashSet<string>(System.Array.ConvertAll(only.Split(','), x => x.Trim()));
    rows = rows.FindAll(r => wanted.Contains(r[0]));
}

if (!System.IO.Directory.Exists(folder))
{
    System.IO.Directory.CreateDirectory(folder);
    UnityEditor.AssetDatabase.ImportAsset("Assets/World/ClassGear", UnityEditor.ImportAssetOptions.ImportRecursive);
}

UnityEngine.Color Hex(string h) { UnityEngine.ColorUtility.TryParseHtmlString(h, out var c); return c; }
int made = 0, updated = 0;
UnityEditor.AssetDatabase.StartAssetEditing();
try
{
    foreach (var r in rows)
    {
        var path = folder + "/" + r[0] + ".mat";
        var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        var isNew = !material;
        if (isNew) material = new UnityEngine.Material(template);
        else { material.shader = template.shader; material.CopyPropertiesFromMaterial(template); }
        material.name = r[0];
        material.SetColor("_Color_Primary", Hex(r[1]));
        material.SetColor("_Color_Secondary", Hex(r[2]));
        material.SetColor("_Color_Leather_Primary", Hex(r[3]));
        material.SetColor("_Color_Leather_Secondary", Hex(r[4]));
        material.SetColor("_Color_Metal_Primary", Hex(r[5]));
        material.SetColor("_Color_Metal_Secondary", Hex(r[6]));
        material.SetColor("_Color_Metal_Dark", Hex(r[7]));
        material.SetColor("_Color_Skin", Hex(skin));
        material.SetFloat("_Metallic", float.Parse(r[8], inv));
        material.SetFloat("_Smoothness", float.Parse(r[9], inv));
        // no glow: it tints the whole piece
        material.SetFloat("_Glow_Enabled", 0f);
        material.SetFloat("_Glow_Factor", 0f);
        material.SetFloat("_Emission", 0f);
        if (material.HasProperty("_Emmision")) material.SetFloat("_Emmision", 0f);
        if (material.HasProperty("_BodyArt_Amount")) material.SetFloat("_BodyArt_Amount", 0f);
        material.enableInstancing = template.enableInstancing;
        if (isNew) { UnityEditor.AssetDatabase.CreateAsset(material, path); made++; }
        else { UnityEditor.EditorUtility.SetDirty(material); updated++; }
    }
}
finally
{
    UnityEditor.AssetDatabase.StopAssetEditing();
}
// only these are saved: anything else that is unsaved in the editor stays as it is
foreach (var r in rows)
{
    var saved = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(folder + "/" + r[0] + ".mat");
    if (saved) UnityEditor.AssetDatabase.SaveAssetIfDirty(saved);
}
return made + " materials made, " + updated + " updated, in " + folder;
