using UnityEngine;
using Verse;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public class Gizmo_Gui : Gizmo
    {
        public Gizmo_Gui()
        {
            this.Order = -1000f;
        }
        public override float GetWidth(float maxWidth) => 154f;

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect rackground = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
            if (Mouse.IsOver(rackground))
            {
                TipSignal tip = "Fuel:{0}".Formatted(core.fuelInt.ToString("0.##"));
                TooltipHandler.TipRegion(rackground, tip);
            }
            Widgets.DrawWindowBackground(rackground);
			Rect shield = new Rect(topLeft.x + 11, topLeft.y + 10, 109, 24);
			Rect health = new Rect(topLeft.x + 11, topLeft.y + 43, 109, 24);
            Rect fuel = new Rect(topLeft.x + 125, topLeft.y + 10, 20, 57);
            Text.Font = GameFont.Tiny;
			float shieldFillPercent = core.Shield / core.ShieldMax;
			float healthFillPercent = core.Health / core.HealthMax;
            float fuelFillPercent = core.fuelInt / 1000f;
            FillableBarByRot4(shield, shieldFillPercent, Rot4.East, shieldBar, black);
			FillableBarByRot4(health, healthFillPercent, Rot4.East, healthBar, black);
            FillableBarByRot4(fuel, fuelFillPercent, Rot4.North, fuelBar, black);
            Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(shield, core.Shield.ToString("0.#") + " / " + core.ShieldMax.ToString());
			Widgets.Label(health, core.Health.ToString("0.#") + " / " + core.HealthMax.ToString());
			Text.Anchor = TextAnchor.UpperLeft;
			return new GizmoResult(GizmoState.Clear);
		}
		public WalkerGear_Core core;
        public static readonly Texture2D healthBar = SolidColorMaterials.NewSolidColorTexture(new Color32(255, 0, 0, 255));
        public static readonly Texture2D shieldBar = SolidColorMaterials.NewSolidColorTexture(new Color32(128, 128, 128, 255));
        public static readonly Texture2D fuelBar = SolidColorMaterials.NewSolidColorTexture(new Color32(255, 149, 0, 255));
        public static readonly Texture2D black = SolidColorMaterials.NewSolidColorTexture(new Color32(0, 0, 0, 255));
        public static Rect FillableBarByRot4(Rect rect, float fillPercent, Rot4 rotation, Texture2D fillTex, Texture2D bgTex = null, bool doBorder = false, float borderSize = 3f, Texture2D borderTex = null)
        {
            if (doBorder)
            {
                GUI.DrawTexture(rect, borderTex ?? BaseContent.BlackTex);
                rect = rect.ContractedBy(borderSize);
            }
            if (bgTex != null)
            {
                GUI.DrawTexture(rect, bgTex);
            }
            Rect result = rect;
            if (rotation == Rot4.East || rotation == Rot4.West)
            {
                if (rotation == Rot4.West)
                {
                    rect.x += rect.width;
                    fillPercent *= -1f;
                }
                rect.width *= fillPercent;
            }
            else//f(rotation == Rot4.East || rotation == Rot4.West)
            {
                if (rotation == Rot4.North)
                {
                    rect.y += rect.height;
                    fillPercent *= -1f;
                }
                rect.height *= fillPercent;
            }
            GUI.DrawTexture(rect, fillTex);
            return result;
        }
    }
}
