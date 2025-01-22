# Expanded Storage

Expanded Storage is a framework mod for adding new types of chests to the game.

## Contents

- [Expanded Storage](#expanded-storage)
  - [Contents](#contents)
  - [Data Format](#data-format)
  - [Texture](#texture)
  - [Example](#example)
  - [Translations](#translations)

## Data Format

<table>
<thead>
<tr>
<th>Field</th>
<th>Description</th>
<th>Type</th>
</tr>
</thead>
<tbody>
<tr>
<td>Animation</td>
<td>An optional animation style that causes the lid animation to constantly loop. Default <code>"None"</code>.</td>
<td>None or Loop</td>
</tr>
<tr>
<td><a href="https://stardewvalleywiki.com/Modding:Audio">CloseNearbySound</a></td>
<td>A sound to play if OpenNearby is true, and the lid closing animation is playing. Default <code>"doorCreakReverse"</code>.</td>
<td>Text</td>
</tr>
<tr>
<td>Frames</td>
<td>The number of frames used for the lid animation. Default <code>1</code>.</td>
<td>Number</td>
</tr>
<tr>
<td><a href="https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6#Global_inventories">GlobalInventoryId</a></td>
<td>An id that causes all chests created with the same id to share the same inventory.</td>
<td>Text</td>
</tr>
<tr>
<td>IsFridge</td>
<td>When true, items in the chest can be used for cooking. Default <code>false</code>.</td>
<td>Boolean</td>
</tr>
<tr>
<td>ModData</td>
<td>Mod data to add to chests when they are created.</td>
<td>Dictionary</td>
</tr>
<tr>
<td>OpenNearby</td>
<td>When true, the lid opening animation plays when the player is within 1 tile of the chest.. Default <code>false</code>.</td>
<td>Boolean</td>
</tr>
<tr>
<td><a href="https://stardewvalleywiki.com/Modding:Audio">OpenNearbySound</a></td>
<td>A sound to play if OpenNearby is true, and the lid opening animation is playing. Default <code>"doorCreak"</code>.</td>
<td>Text</td>
</tr>
<tr>
<td><a href="https://stardewvalleywiki.com/Modding:Audio">OpenSound</a></td>
<td>A sound to play when the player opens a chest by clicking on it. Default <code>"openChest"</code>.</td>
<td>Text</td>
</tr>
<tr>
<td><a href="https://stardewvalleywiki.com/Modding:Audio">PlaceSound</a></td>
<td>A sound to play when the chest is placed in the world. Default <code>"axe"</code>.</td>
<td>Text</td>
</tr>
<tr>
<td>PlayerColor</td>
<td>When true, allows the use of the color picker for the chest. Default <code>false</code>.</td>
<td>Boolean</td>
</tr>
<tr>
<td>SpecialChestType</td>
<td>Allows the chest to be created as one of the special chest types. Default <code>"None"</code>. Another option is BigChest for the chests to be created as a <a href="https://stardewvalleywiki.com/Big_Chest">large chest</a>.</td>
<td>Text</td>
</tr>
<tr>
<td>TintOverride</td>
<td>A list of color values to customize the color picker with. (Requires Colorful Chests).</td>
<td>List of Colors</td>
</tr>
</tbody>
</table>

## Texture

Expanded Storage requires the sprite sheet to be in a specific layout.

- Each sprite sheet can contain sprites for only one chest.
- Each sprite is 16x32.
- Lid animations go from left-to-right, and must match the number of Frames.

<table>
<thead>
<tr>
<th>Row</th>
<th>Description</th>
<th>Required</th>
</tr>
</thead>
<tbody>
<tr>
<td>1</td>
<td>The default sprite when a player color is not selected.</td>
<td>Always</td>
</tr>
<tr>
<td>2</td>
<td>When a player color choice is selected, this sprite is tinted.</td>
<td>Only if PlayerColor is true</td>
</tr>
<tr>
<td>3</td>
<td>When a player color choice is selected, this sprite is drawn above Row 2 without the tint</td>
<td>Only if PlayerColor is true</td>
</tr>
<tr>
<td>4-23</td>
<td>Each of these corresponds to one of the color palette choices. When provided, this will be used instead of Row 3.</td>
<td>Never</td>
</tr>
</tbody>
</table>

## Example

```json
{
  "Format": "2.4.0",
  "Changes": [
    {
      "LogName": "Load the custom chest",
      "Action": "EditData",
      "Target": "Data/BigCraftables",
      "Entrie": {
        "{{ModId}}_MyChest": {
          "Name": "MyChest",
          "DisplayName": "{{i18n: MyChest.name}}",
          "Description": "{{i18n: MyChest.description}}",
          "Texture": "{{InternalAssetKey: assets/MyChest.png}}",
          "CustomFields": {
            "furyx639.ExpandedStorage/IsFridge": "true",
            "furyx639.ExpandedStorage/PlayerColor": "true",
            "furyx639.ExpandedStorage/OpenSound": "Ship"
          }
        }
      }
    }
  ]
}
```

## Translations

❌️ = Not Translated, ❔ = Incomplete, ✔️ = Complete

|            |         Expanded Storage          |
| :--------- | :-------------------------------: |
| Chinese    | [✔️](ExpandedStorage/i18n/zh.json) |
| French     | [✔️](ExpandedStorage/i18n/fr.json) |
| German     | [✔️](ExpandedStorage/i18n/de.json) |
| Hungarian  | [✔️](ExpandedStorage/i18n/hu.json) |
| Italian    | [✔️](ExpandedStorage/i18n/it.json) |
| Japanese   | [✔️](ExpandedStorage/i18n/ja.json) |
| Korean     | [✔️](ExpandedStorage/i18n/ko.json) |
| Portuguese | [✔️](ExpandedStorage/i18n/pt.json) |
| Russian    | [✔️](ExpandedStorage/i18n/ru.json) |
| Spanish    | [✔️](ExpandedStorage/i18n/es.json) |
| Turkish    | [✔️](ExpandedStorage/i18n/tr.json) |
