# Immersive Orbital Traders — Wiki

Immersive Orbital Traders is a RimWorld 1.6 mod **framework** that adds a portrait panel and procedural lore to the trade dialog. When a player opens trade with an orbital ship, the mod selects a matching character definition, renders a portrait beside the dialog, and generates a short biography using RimWorld's grammar system.

The mod ships with 16 built-in characters covering RimWorld's xenotype roster, but its primary purpose is to be extended. Any other mod can add new characters, override existing ones, or disable unwanted ones through standard XML.

---

## How It Works

1. A player contacts an orbital trader.
2. The mod picks a random `OrbitalTraderCharacterDef` from the loaded def database. Defs can be restricted to specific trader kinds or kept global (eligible for any trader).
3. The portrait texture is drawn in a panel to the right of the trade window.
4. A trader name is generated deterministically from the trader's internal seed, so the same ship always shows the same character within a play session.
5. Lore text is resolved via a `RulePackDef` grammar tree. If no rule pack is defined, a static fallback string is shown instead.

---

## Requirements

| Dependency | Where to Get |
|---|---|
| [Harmony](https://github.com/pardeike/HarmonyRimWorld) | Steam Workshop / GitHub |
| RimWorld 1.6 | Steam |

---

## Wiki Pages

- **[Creating a Custom Character](Creating-a-Custom-Character)** — How to add a new character portrait and lore to the framework.
- **[Disabling Characters with Patches](Disabling-Characters-with-Patches)** — How to turn off built-in characters without editing the original mod files.
- **[RulePackDef Guide](RulePackDef-Guide)** — How RimWorld's grammar rule system works and how to write lore text files for it.

---

## Mod Settings

One setting is available under **Options > Mod Settings > Immersive Orbital Traders**:

**Show portraits for trader caravans** (default: off) — Trader caravans already have visible pawns on the map. Enabling this may produce a mismatch between the portrait and the actual pawn, which can break immersion. Leave it off unless you specifically want caravan support.

---

## Package ID

`sk.iotframework`
