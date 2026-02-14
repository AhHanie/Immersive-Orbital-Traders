using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ImmersiveOrbitalTraders
{
    [HarmonyPatch(typeof(Dialog_Trade), nameof(Dialog_Trade.DoWindowContents))]
    public static class DialogTrade_DoWindowContents_Patch
    {
        private const float PanelWidth = 300f;
        private const float PanelGap = 0f;
        private const float PanelLeftOverlap = 1f;
        private const float PanelTopFactor = 0.20f;
        private const float PanelBottomFactor = 0.80f;
        private const float PanelPadding = 10f;
        private const float PortraitSize = 256f;
        private const float LoreTopSpacing = 10f;

        private static readonly ConditionalWeakTable<ITrader, TraderCharacterSelection> SelectionByTrader = new ConditionalWeakTable<ITrader, TraderCharacterSelection>();
        private static readonly DialogTradePortraitState CurrentState = new DialogTradePortraitState();
        private static Dialog_Trade activeDialog;

        private static void Prefix(Dialog_Trade __instance)
        {
            EnsureDialogState(__instance);
            DialogTradePortraitState state = CurrentState;
            EnsureInitialized(state);

            if (state.SelectedCharacter == null || state.PortraitGraphic == null)
            {
                return;
            }

            EnsurePanelWindow(__instance, state);
        }

        private static void EnsureInitialized(DialogTradePortraitState state)
        {
            if (state.Initialized)
            {
                return;
            }

            ITrader trader = state.Trader ?? ResolveCurrentTrader();

            state.Trader = trader;

            if (!IsEligibleTrader(trader))
            {
                state.Initialized = true;
                string traderType = trader.GetType().Name;
                Logger.Message("Skipping portrait panel for non-orbital trader type: " + traderType);
                return;
            }

            TraderKindDef traderKind = trader.TraderKind;
            state.TraderKind = traderKind;
            state.Initialized = true;

            if (!TryGetOrCreateSelection(trader, traderKind, out OrbitalTraderCharacterDef selected, out Graphic portraitGraphic, out string generatedName, out string resolvedLore, out bool loreResolved))
            {
                return;
            }

            state.SelectedCharacter = selected;
            state.PortraitGraphic = portraitGraphic;
            state.ResolvedLore = resolvedLore;
            state.LoreResolved = loreResolved;
            state.LoreScrollPosition = Vector2.zero;
            Logger.Message("Selected trader character '" + selected.defName + "' for trader kind '" + traderKind.defName + "'.");
        }

        private static bool TryGetOrCreateSelection(ITrader trader, TraderKindDef traderKind, out OrbitalTraderCharacterDef selected, out Graphic portraitGraphic, out string generatedName, out string resolvedLore, out bool loreResolved)
        {
            selected = null;
            portraitGraphic = null;
            generatedName = string.Empty;
            resolvedLore = string.Empty;
            loreResolved = false;

            if (SelectionByTrader.TryGetValue(trader, out TraderCharacterSelection existing))
            {
                selected = existing.Character;
                portraitGraphic = existing.PortraitGraphic;
                generatedName = existing.GeneratedName;
                resolvedLore = existing.ResolvedLore;
                loreResolved = existing.LoreResolved;
                return true;
            }

            selected = OrbitalTraderCharacterManager.GetRandomCharacter(traderKind);
            if (selected == null)
            {
                Logger.Message("No eligible trader character for trader kind: " + traderKind.defName);
                return false;
            }

            portraitGraphic = selected.portraitGraphicData.Graphic;
            if (portraitGraphic == null)
            {
                Logger.Warning("Selected trader character has no usable portrait graphic: " + selected.defName);
                return false;
            }

            resolvedLore = OrbitalTraderCharacterManager.ResolveLoreText(trader, selected, out generatedName);
            loreResolved = !string.IsNullOrWhiteSpace(resolvedLore);

            SelectionByTrader.Remove(trader);
            SelectionByTrader.Add(trader, new TraderCharacterSelection
            {
                Character = selected,
                PortraitGraphic = portraitGraphic,
                GeneratedName = generatedName,
                ResolvedLore = resolvedLore,
                LoreResolved = loreResolved
            });
            return true;
        }

        private static void EnsurePanelWindow(Dialog_Trade dialog, DialogTradePortraitState state)
        {
            if (state.PanelWindow != null)
            {
                return;
            }

            Rect panelRect = CalculatePanelRect(dialog, state);
            DialogTradePortraitWindow window = new DialogTradePortraitWindow(dialog, state, panelRect);
            state.PanelWindow = window;
            Find.WindowStack.Add(window);
        }

        private static ITrader ResolveCurrentTrader()
        {
            return TradeSession.trader;
        }

        private static bool IsEligibleTrader(ITrader trader)
        {
            if (ModSettings.AllowTraderCaravans)
            {
                return true;
            }

            if (trader is TradeShip)
            {
                return true;
            }

            return false;
        }

        private static Rect CalculatePanelRect(Dialog_Trade dialog, DialogTradePortraitState state)
        {
            Rect tradeRect = dialog.windowRect;
            float maxPanelHeight = Mathf.Clamp(tradeRect.height * (PanelBottomFactor - PanelTopFactor), 280f, tradeRect.height);

            float contentWidth = PanelWidth - (PanelPadding * 2f);
            float portraitDrawSize = Mathf.Min(PortraitSize, contentWidth);

            string loreText = GetDisplayLoreText(state);
            float loreTextHeight = Text.CalcHeight(loreText, Mathf.Max(1f, contentWidth - 18f));

            float neededHeight = (PanelPadding * 2f) + portraitDrawSize + LoreTopSpacing + loreTextHeight + 4f;
            float panelTop = tradeRect.y + (tradeRect.height * PanelTopFactor);
            float panelBottom = tradeRect.y + (tradeRect.height * PanelBottomFactor);
            float availableHeightFromTop = Mathf.Max(0f, panelBottom - panelTop);
            float maxAllowedHeight = Mathf.Min(maxPanelHeight, availableHeightFromTop);
            float panelHeight = Mathf.Min(neededHeight, maxAllowedHeight);
            state.LoreIsHeightCapped = neededHeight > maxAllowedHeight + 0.1f;
            return new Rect(tradeRect.xMax + PanelGap - PanelLeftOverlap, panelTop, PanelWidth, panelHeight);
        }

        internal static void EnsureDialogState(Dialog_Trade dialog)
        {
            if (activeDialog == dialog)
            {
                return;
            }

            PrepareState(dialog, ResolveCurrentTrader());
        }

        internal static void PrepareState(Dialog_Trade dialog, ITrader trader)
        {
            if (activeDialog != null && activeDialog != dialog)
            {
                ClearState(activeDialog);
            }

            activeDialog = dialog;

            if (CurrentState.PanelWindow != null)
            {
                DialogTradePortraitWindow panel = CurrentState.PanelWindow;
                CurrentState.PanelWindow = null;
                panel.Close(doCloseSound: false);
            }

            CurrentState.Initialized = false;
            CurrentState.LoreIsHeightCapped = false;
            CurrentState.LoreResolved = false;
            CurrentState.LoreScrollPosition = Vector2.zero;
            CurrentState.SelectedCharacter = null;
            CurrentState.PortraitGraphic = null;
            CurrentState.ResolvedLore = string.Empty;

            CurrentState.Trader = trader;
            CurrentState.TraderKind = trader.TraderKind;
        }

        internal static void ClearState(Dialog_Trade dialog)
        {
            if (dialog == null || activeDialog != dialog)
            {
                return;
            }

            if (CurrentState.PanelWindow != null)
            {
                DialogTradePortraitWindow panel = CurrentState.PanelWindow;
                CurrentState.PanelWindow = null;
                panel.Close(doCloseSound: false);
            }

            CurrentState.Initialized = false;
            CurrentState.LoreIsHeightCapped = false;
            CurrentState.LoreResolved = false;
            CurrentState.LoreScrollPosition = Vector2.zero;
            CurrentState.Trader = null;
            CurrentState.TraderKind = null;
            CurrentState.SelectedCharacter = null;
            CurrentState.PortraitGraphic = null;
            CurrentState.ResolvedLore = string.Empty;
            activeDialog = null;
        }

        private static string GetDisplayLoreText(DialogTradePortraitState state)
        {
            if (state.LoreResolved && !string.IsNullOrWhiteSpace(state.ResolvedLore))
            {
                return state.ResolvedLore;
            }

            return "ImmersiveOrbitalTraders.NoLoreLabel".Translate().ToString();
        }

        public sealed class DialogTradePortraitWindow : Window
        {
            private readonly Dialog_Trade parentDialog;
            private readonly DialogTradePortraitState state;

            public DialogTradePortraitWindow(Dialog_Trade parentDialog, DialogTradePortraitState state, Rect initialRect)
            {
                this.parentDialog = parentDialog;
                this.state = state;

                absorbInputAroundWindow = false;
                closeOnAccept = false;
                closeOnCancel = false;
                doCloseX = false;
                doCloseButton = false;
                draggable = false;
                resizeable = false;
                preventCameraMotion = false;
                focusWhenOpened = false;
                onlyOneOfTypeAllowed = false;

                layer = WindowLayer.SubSuper;
                windowRect = initialRect;
                doWindowBackground = true;
            }

            public override Vector2 InitialSize => new Vector2(windowRect.width, windowRect.height);
            protected override float Margin => 0f;

            public override void PostOpen()
            {
                base.PostOpen();
                windowRect = CalculatePanelRect(parentDialog, state);
            }

            public override void DoWindowContents(Rect inRect)
            {
                // Handle a race condition where closing the trade window, doesn't close the side panel at the same time
                if (state.PortraitGraphic == null)
                {
                    return;
                }
                windowRect = CalculatePanelRect(parentDialog, state);
                Widgets.DrawBoxSolid(new Rect(inRect.x, inRect.y, 2f, inRect.height), Widgets.WindowBGFillColor);

                Rect contentRect = inRect.ContractedBy(PanelPadding);
                float drawPortraitSize = Mathf.Min(PortraitSize, contentRect.width);
                Rect portraitRect = new Rect(contentRect.x + (contentRect.width - drawPortraitSize) * 0.5f, contentRect.y, drawPortraitSize, drawPortraitSize);

                Texture portraitTexture = state.PortraitGraphic.MatSingle.mainTexture;
                if (portraitTexture != null)
                {
                    Widgets.DrawTextureFitted(portraitRect, portraitTexture, 1f);
                }

                float loreTop = portraitRect.yMax + LoreTopSpacing;
                float loreHeight = contentRect.yMax - loreTop;
                if (loreHeight <= 0f)
                {
                    return;
                }

                Rect loreOutRect = new Rect(portraitRect.x, loreTop, portraitRect.width, loreHeight);
                string loreText = GetDisplayLoreText(state);

                float viewWidth = loreOutRect.width - 18f;
                float loreTextHeight = Text.CalcHeight(loreText, viewWidth);
                float requiredHeight = loreTextHeight + 4f;

                if (!state.LoreIsHeightCapped || requiredHeight <= loreOutRect.height)
                {
                    state.LoreScrollPosition = Vector2.zero;
                    Widgets.Label(new Rect(loreOutRect.x, loreOutRect.y, loreOutRect.width, requiredHeight), loreText);
                }
                else
                {
                    Rect loreViewRect = new Rect(0f, 0f, viewWidth, requiredHeight);
                    Widgets.BeginScrollView(loreOutRect, ref state.LoreScrollPosition, loreViewRect);
                    Widgets.Label(new Rect(0f, 0f, loreViewRect.width, requiredHeight), loreText);
                    Widgets.EndScrollView();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Dialog_Trade), MethodType.Constructor, typeof(Pawn), typeof(ITrader), typeof(bool))]
    public static class DialogTrade_Constructor_Patch
    {
        private static void Postfix(Dialog_Trade __instance, ITrader trader)
        {
            DialogTrade_DoWindowContents_Patch.PrepareState(__instance, trader);
        }
    }

    [HarmonyPatch(typeof(Window), nameof(Window.Close))]
    public static class Window_Close_Patch
    {
        private static void Prefix(Window __instance)
        {
            if (__instance is Dialog_Trade dialog)
            {
                DialogTrade_DoWindowContents_Patch.ClearState(dialog);
            }
        }
    }

    public sealed class DialogTradePortraitState
    {
        public bool Initialized;
        public bool LoreIsHeightCapped;
        public bool LoreResolved;
        public Vector2 LoreScrollPosition;
        public ITrader Trader;
        public TraderKindDef TraderKind;
        public OrbitalTraderCharacterDef SelectedCharacter;
        public Graphic PortraitGraphic;
        public string ResolvedLore;
        public DialogTrade_DoWindowContents_Patch.DialogTradePortraitWindow PanelWindow;
    }

    public sealed class TraderCharacterSelection
    {
        public OrbitalTraderCharacterDef Character;
        public Graphic PortraitGraphic;
        public string GeneratedName;
        public string ResolvedLore;
        public bool LoreResolved;
    }
}
