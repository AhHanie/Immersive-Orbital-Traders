# Creating a Custom Character

This guide walks through every step required to add a new trader character to the framework. No C# is needed — everything is done in XML and plain text files.

---

## Overview of Required Files

| File                                                              | Purpose                                         |
| ----------------------------------------------------------------- | ----------------------------------------------- |
| `Textures/YourMod/MyTrader.png`                                   | Portrait image                                  |
| `1.6/Defs/OrbitalTraderCharacters/MyTrader.xml`                   | Character definition                            |
| `1.6/Defs/RulePacks/MyTrader_RulePacks.xml`                       | Grammar rule pack (optional but recommended)    |
| `1.6/Languages/English/Strings/YourMod/TraderLore/MyTrader/*.txt` | Lore text lines (required if using a rule pack) |

---

## Step 1: Prepare the Portrait

Create a square PNG image for the trader portrait. The mod renders it at up to 256×256 pixels inside the panel, so 256×256 is the recommended size.

Place the file under your mod's `Textures` folder:

```
YourMod/
  Textures/
    YourMod/
      MyTrader.png
```

The texture path used in the def is relative to the `Textures` folder and omits the file extension:

```
YourMod/MyTrader
```

---

## Step 2: Create the Character Def

Create an XML file anywhere under `1.6/Defs/`. A dedicated subfolder like `OrbitalTraderCharacters/` is recommended for organisation.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <ImmersiveOrbitalTraders.OrbitalTraderCharacterDef>
    <defName>MYMOD_MyTrader</defName>

    <portraitGraphicData>
      <texPath>YourMod/MyTrader</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </portraitGraphicData>

    <loreRulePack>MyTrader_Lore</loreRulePack>
    <loreText>A veteran hauler who has crossed the rim twice and never lost a shipment.</loreText>
  </ImmersiveOrbitalTraders.OrbitalTraderCharacterDef>
</Defs>
```

### Field Reference

#### `defName` _(required)_

Unique identifier for this def. Use a prefix that is unlikely to conflict with other mods (e.g. your mod's abbreviation). The built-in defs use the `IOT_` prefix.

---

#### `portraitGraphicData` _(required)_

Defines the portrait texture. It is a standard RimWorld `GraphicData` block.

| Sub-field      | Required | Description                                                              |
| -------------- | -------- | ------------------------------------------------------------------------ |
| `texPath`      | Yes      | Path to the PNG relative to the `Textures` folder, without extension.    |
| `graphicClass` | No       | Always use `Graphic_Single`. The mod sets this automatically if omitted. |

---

#### `loreRulePack` _(required if `loreText` is absent)_

The `defName` of a `RulePackDef` that generates the lore text. When a trader is opened, the grammar system resolves the `root` rule from this pack and displays the result. See [RulePackDef Guide](RulePackDef-Guide) for details on writing one.

If the rule pack fails to resolve for any reason, the mod falls back to `loreText`.

---

#### `loreText` _(required if `loreRulePack` is absent)_

A static fallback string shown when no rule pack is defined or when rule resolution fails. You can use this as the sole lore source if procedural generation is not needed.

Both `loreRulePack` and `loreText` can be set at the same time. The rule pack takes priority; `loreText` acts as the safety net.

---

#### `allowedTraderKinds` _(optional)_

A list of `TraderKindDef` names that restrict which traders can use this character. When absent (or empty), the character is **global** and can appear for any orbital trader.

```xml
<allowedTraderKinds>
  <li>Bulk_Outlander</li>
  <li>Slaver_Pirate</li>
</allowedTraderKinds>
```

When a trade dialog opens, the mod builds a candidate list from all global defs plus any defs whose `allowedTraderKinds` match the current trader. A random one is chosen from that merged list.

---

#### `gender` _(optional, default: `Male`)_

The gender used when generating the trader's name. Accepts any RimWorld `Gender` value: `Male`, `Female`, or `None`.

```xml
<gender>Female</gender>
```

When set to `None`, a gender-neutral name is generated.

---

#### `disabled` _(optional, default: `false`)_

When set to `true`, this def is ignored entirely during character selection. The character will not appear in trade dialogs. Intended for patching — see [Disabling Characters with Patches](Disabling-Characters-with-Patches).

```xml
<disabled>true</disabled>
```

---

## Step 3: Create the Rule Pack (Recommended)

If you want procedurally varied lore, create a `RulePackDef` and a set of text files. Skip to Step 4 if you are using a static `loreText` only.

Create an XML file under `1.6/Defs/RulePacks/`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <RulePackDef>
    <defName>MyTrader_Lore</defName>
    <rulePack>
      <rulesFiles>
        <li>root->YourMod/TraderLore/MyTrader/root</li>
        <li>intro->YourMod/TraderLore/MyTrader/intro</li>
        <li>history->YourMod/TraderLore/MyTrader/history</li>
        <li>closing->YourMod/TraderLore/MyTrader/closing</li>
      </rulesFiles>
    </rulePack>
  </RulePackDef>
</Defs>
```

Each `<li>` entry maps a **rule name** to a **text file path** (relative to `1.6/Languages/English/Strings/`, without the `.txt` extension). See [RulePackDef Guide](RulePackDef-Guide) for the full syntax.

---

## Step 4: Write the Lore Text Files

Create one `.txt` file per rule under:

```
1.6/Languages/English/Strings/YourMod/TraderLore/MyTrader/
```

**`root.txt`** — The entry point rule. Combines the other rules into a full paragraph. The grammar resolver always starts here.

```
[intro] [history] [closing]
```

**`intro.txt`** — One or more lines, each a different possible introduction sentence. The resolver picks one at random.

```
[name] is a veteran hauler operating under [faction] contracts.
[name] runs long-haul cargo routes on behalf of [faction].
```

**`history.txt`**

```
Since [startYear], [name] has made this crossing without a single failed delivery.
The route logs show unbroken service stretching back to [startYear].
```

**`closing.txt`**

```
If the price is right, [name] will deal.
Negotiations are brief and final.
```

### Available Variables

The mod injects three variables into every grammar request. You can reference them anywhere in your text files.

| Variable      | Description                                                                          | Example          |
| ------------- | ------------------------------------------------------------------------------------ | ---------------- |
| `[name]`      | A procedurally generated first + last name, seeded deterministically per trader.     | `Mira Venn`      |
| `[faction]`   | The trader's faction name, or `independent operators` if none.                       | `OutlanderUnion` |
| `[startYear]` | A year calculated from the game's current year minus 1–12 years (seeded per trader). | `5482`           |

---
