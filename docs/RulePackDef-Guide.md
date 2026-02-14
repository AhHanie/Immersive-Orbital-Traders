# RulePackDef Guide

RimWorld uses a grammar system to generate procedural text, pawn backstories, art descriptions, incident messages, and more. Immersive Orbital Traders uses the same system to produce unique trader lore. This page explains how `RulePackDef` works so you can write your own.

---

## What Is a RulePackDef?

A `RulePackDef` is a named def that holds a collection of **grammar rules**. Each rule has a name and a set of possible values. When the grammar resolver is asked to resolve a rule (e.g. `root`), it picks one of that rule's values at random, then recursively resolves any rule references embedded within it.

The end result is a flat string, the generated lore text.

---

## Inline Rules with `rulesStrings`

The simplest way to define a rule pack is entirely inline inside the XML def. Each `<li>` inside `<rulesStrings>` is formatted as:

```
ruleName->possible value text
```

The same rule name can appear on multiple lines. The resolver picks one value at random each time that rule is needed.

Here is a complete self-contained example:

```xml
<RulePackDef>
  <defName>MyTrader_Lore</defName>
  <rulePack>
    <rulesStrings>
      <li>root->[intro] [history] [closing]</li>

      <li>intro->[name] is a veteran hauler operating under [faction] contracts.</li>
      <li>intro->[name] runs long-haul cargo routes on behalf of [faction].</li>

      <li>history->Since [startYear], [name] has made this crossing without a single failed delivery.</li>
      <li>history->The route logs show unbroken service stretching back to [startYear].</li>

      <li>closing->If the price is right, [name] will deal.</li>
      <li>closing->Negotiations are brief and final.</li>
    </rulesStrings>
  </rulePack>
</RulePackDef>
```

This pack can generate four different combinations from the `history` and `closing` lines alone, and the names and dates vary per trader.

---

## The Root Rule

The grammar resolver always starts from a rule named **`root`**. This is the entry point. It is typically a template that references the other rules by name:

```xml
<li>root->[intro] [history] [closing]</li>
```

When resolved, this picks one value from `intro`, one from `history`, and one from `closing`, then joins them with spaces.

If `root` references a rule name that is not defined, the resolver leaves the placeholder unreplaced (or substitutes an error string in debug mode).

---

## Rule References

A rule name wrapped in square brackets is a **reference**. When the resolver encounters `[intro]`, it looks up the `intro` rule and substitutes one of its values.

References can be nested. A value in one rule can itself reference another:

```xml
<li>intro->[name] is a [traderType] working for [faction].</li>

<li>traderType->hauler</li>
<li>traderType->salvager</li>
<li>traderType->arms dealer</li>
```

This produces combinations like:

- `Mira Venn is a hauler working for OutlanderUnion.`
- `Mira Venn is a salvager working for OutlanderUnion.`

---

## Variables Injected by the Mod

Immersive Orbital Traders injects three **constants** into every grammar request before resolving. Constants behave like rules with a single fixed value — they are always resolved the same way for a given trader.

| Variable      | Description                                                                                                                     |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `[name]`      | A generated trader name (e.g. `Mira Venn`). Seeded per trader, so the same ship always produces the same name within a session. |
| `[faction]`   | The trader's faction name. Falls back to `independent operators` if the trader has no faction.                                  |
| `[startYear]` | A past year (current game year minus 1–12 years, seeded per trader). Useful for backstory flavour.                              |

These can be used in any rule without declaring them in the def.

---

## Loading Rules from Text Files

Inline rules work fine for short packs, but become unwieldy when you want many variants per rule. The cleaner approach is `<rulesFiles>`, which loads each rule's values from a separate `.txt` file.

Each `<li>` maps a rule name to a file path:

```
ruleName->path/relative/to/Strings/folder
```

The path is relative to `1.6/Languages/English/Strings/` and does **not** include the `.txt` extension.

```xml
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
```

The corresponding files sit at:

```
1.6/Languages/English/Strings/YourMod/TraderLore/MyTrader/root.txt
1.6/Languages/English/Strings/YourMod/TraderLore/MyTrader/intro.txt
1.6/Languages/English/Strings/YourMod/TraderLore/MyTrader/history.txt
1.6/Languages/English/Strings/YourMod/TraderLore/MyTrader/closing.txt
```

Each **line** in a `.txt` file becomes one possible value for that rule. The resolver picks one line at random.

**`root.txt`:**

```
[intro] [history] [closing]
```

**`intro.txt`:**

```
[name] is a veteran hauler operating under [faction] contracts.
[name] runs long-haul cargo routes on behalf of [faction].
```

**`history.txt`:**

```
Since [startYear], [name] has made this crossing without a single failed delivery.
The route logs show unbroken service stretching back to [startYear].
```

**`closing.txt`:**

```
If the price is right, [name] will deal.
Negotiations are brief and final.
```

---

## Combining `rulesStrings` and `rulesFiles`

Both can coexist in the same `rulePack`. This is useful when most rules live in files but you want a short inline list for something like a job title:

```xml
<rulePack>
  <rulesFiles>
    <li>root->YourMod/TraderLore/MyTrader/root</li>
    <li>intro->YourMod/TraderLore/MyTrader/intro</li>
    <li>history->YourMod/TraderLore/MyTrader/history</li>
    <li>closing->YourMod/TraderLore/MyTrader/closing</li>
  </rulesFiles>
  <rulesStrings>
    <li>traderType->hauler</li>
    <li>traderType->salvager</li>
    <li>traderType->courier</li>
  </rulesStrings>
</rulePack>
```

---

## Real Example from This Mod

**Def (`TraderLore_AndroidTrader`):**

```xml
<RulePackDef>
  <defName>TraderLore_AndroidTrader</defName>
  <rulePack>
    <rulesFiles>
      <li>root->ImmersiveOrbitalTraders/TraderLore/TraderLore_AndroidTrader/root</li>
      <li>intro->ImmersiveOrbitalTraders/TraderLore/TraderLore_AndroidTrader/intro</li>
      <li>history->ImmersiveOrbitalTraders/TraderLore/TraderLore_AndroidTrader/history</li>
      <li>edge->ImmersiveOrbitalTraders/TraderLore/TraderLore_AndroidTrader/edge</li>
      <li>closing->ImmersiveOrbitalTraders/TraderLore/TraderLore_AndroidTrader/closing</li>
    </rulesFiles>
  </rulePack>
</RulePackDef>
```

**`root.txt`:**

```
[intro] [history] [edge] [closing]
```

**`intro.txt`:**

```
[name] is a machine quartermaster licensed by [faction].
Under [faction] registry codes, [name] handles orbital manifests without deviation.
```

A resolved result might be:

> _K-7 Vance is a machine quartermaster licensed by OutlanderUnion. Its memory core tracks contracts back to 5480 and flags every broken promise. Colonies trust its counts because it audits every crate three times before docking. If the manifest matches, the deal is made._

---
