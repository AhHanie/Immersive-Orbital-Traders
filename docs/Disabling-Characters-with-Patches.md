# Disabling Characters with Patches

Every `OrbitalTraderCharacterDef` has a `disabled` field. When it is `true`, the character is excluded from selection and will never appear in a trade dialog.

This is useful when:

- You want to remove a built-in character from another mod without deleting its files.
- You are replacing a character with your own version and want the original hidden.
- You want to curate which characters appear for a specific modpack.

Editing the original mod's XML directly is always a bad idea — your changes are overwritten whenever the mod updates. Instead, use **RimWorld's XML patch system** to modify the def at load time.

---

## How RimWorld XML Patches Work

RimWorld applies XML patches after all defs are loaded. Patches are placed in:

```
YourMod/
  1.6/
    Patches/
      MyPatch.xml
```

Each patch file contains `<Operation>` nodes that find a def in the database and modify it. The most common operation for this use case is `PatchOperationAdd` (to add the `<disabled>` element) or `PatchOperationReplace` (to replace it if it already exists).

---

## Disabling a Single Built-In Character

The following patch disables `IOT_PirateTraderMale` from the base mod.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>

  <Operation Class="PatchOperationAdd">
    <xpath>/Defs/ImmersiveOrbitalTraders.OrbitalTraderCharacterDef[defName="IOT_PirateTraderMale"]</xpath>
    <value>
      <disabled>true</disabled>
    </value>
  </Operation>

</Patch>
```

**How it works:**

- `xpath` is an XPath expression that locates the target element in the merged def database.
- `/Defs/ImmersiveOrbitalTraders.OrbitalTraderCharacterDef[defName="IOT_PirateTraderMale"]` selects the def whose `<defName>` is exactly `IOT_PirateTraderMale`.
- `PatchOperationAdd` inserts `<disabled>true</disabled>` as a child of that element.

Because `disabled` defaults to `false`, it will not exist in the original XML, so `PatchOperationAdd` is the right choice. If you are patching a def that already has `<disabled>false</disabled>` written out explicitly, use `PatchOperationReplace` instead:

```xml
<Operation Class="PatchOperationReplace">
  <xpath>/Defs/ImmersiveOrbitalTraders.OrbitalTraderCharacterDef[defName="IOT_PirateTraderMale"]/disabled</xpath>
  <value>
    <disabled>true</disabled>
  </value>
</Operation>
```

---

## Disabling Multiple Characters at Once

Use separate `<Operation>` blocks in the same patch file — one per character:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>

  <Operation Class="PatchOperationAdd">
    <xpath>/Defs/ImmersiveOrbitalTraders.OrbitalTraderCharacterDef[defName="IOT_PirateTraderMale"]</xpath>
    <value>
      <disabled>true</disabled>
    </value>
  </Operation>

  <Operation Class="PatchOperationAdd">
    <xpath>/Defs/ImmersiveOrbitalTraders.OrbitalTraderCharacterDef[defName="IOT_PirateTraderFemale"]</xpath>
    <value>
      <disabled>true</disabled>
    </value>
  </Operation>

  <Operation Class="PatchOperationAdd">
    <xpath>/Defs/ImmersiveOrbitalTraders.OrbitalTraderCharacterDef[defName="IOT_WasterTrader"]</xpath>
    <value>
      <disabled>true</disabled>
    </value>
  </Operation>

</Patch>
```

---

## Making a Patch Conditional on Another Mod Being Active

If your patch targets a mod that may not be installed by all users, wrap it in `PatchOperationFindMod` so it only runs when the target mod is present:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>

  <Operation Class="PatchOperationFindMod">
    <mods>
      <li>sk.iotframework</li>
    </mods>
    <match Class="PatchOperationAdd">
      <xpath>/Defs/ImmersiveOrbitalTraders.OrbitalTraderCharacterDef[defName="IOT_PirateTraderMale"]</xpath>
      <value>
        <disabled>true</disabled>
      </value>
    </match>
  </Operation>

</Patch>
```

This is good practice whenever your mod optionally patches another.

---

## Built-In Character defNames

These are the `defName` values for all characters shipped with the base mod:

| defName | Character |
|---|---|
| `IOT_AndroidTrader` | Android quartermaster |
| `IOT_CatTrader` | Feline broker |
| `IOT_DirtmoleTrader` | Underground specialist |
| `IOT_GenieTrader` | Gene-tailored analyst |
| `IOT_HighmateTrader` | Highmate envoy |
| `IOT_HussarTrader` | Battle-bred hauler captain |
| `IOT_ImpidTrader` | Impid merchant |
| `IOT_NeanderthalTrader` | Neanderthal heavy-lift specialist |
| `IOT_PigskinTrader` | Pigskin bulk captain |
| `IOT_PirateTraderFemale` | Former raider turned broker (female) |
| `IOT_PirateTraderMale` | Gray-market convoy operator (male) |
| `IOT_SanguophageTrader` | Ancient sanguophage contract-holder |
| `IOT_StarjackTrader` | Voidsuit salvage veteran |
| `IOT_TraderAlien` | Offworld alien emissary |
| `IOT_WasterTrader` | Toxic-zone courier |
| `IOT_YttakinTrader` | Cold-world negotiator |
