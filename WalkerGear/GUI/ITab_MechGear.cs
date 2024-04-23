using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;
using static UnityEngine.Random;


namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public class ITab_MechGear : ITab
    {
        public ITab_MechGear()
        {
            this.size = new Vector2(512f, 368f);
            this.labelKey = "TabMechGear".Translate();
        }
        private Building_MaintenanceBay parent
        {
            get => SelThing as Building_MaintenanceBay;
        }
        private const float side = 80f;
        private Vector2 GizmoSize
        {
            get => new Vector2(side, side);
        }
        private readonly Color grey = new ColorInt(72, 82, 92).ToColor;

        private static readonly Texture2D EmptySlotIcon = Command.BGTex;

        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 0f, this.size.x, this.size.y);
            Rect inner = rect.ContractedBy(3f);
            //Draw Title
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.UpperRight;
                string title = "MechSolution".Translate();
                Vector2 titleSize = Text.CalcSize(title);
                Rect titleBox = new Rect(new Vector2(inner.xMax - titleSize.x, inner.y), titleSize);
                Widgets.Label(titleBox, title);
                Text.Font = GameFont.Small;

                Text.Anchor = TextAnchor.UpperLeft;
            }
            

            foreach (SlotDef slot in parent.slotDefs)
            {
                Draw_GizmoSlot(slot);
            }
            if (parent.occupiedSlots.ContainsKey(SlotDefOf.Core))
            {
                Vector2 position = positions[0];
                position.x =170f - (side / 5f);
                position.y = 56f+(side * 2 + 5f);
                Vector2 box = GizmoSize * 2f;
                box.x *= 1.1f;
                box.y = size.y - position.y - 10f;
                Rect statBlock = new Rect(position, box);
                DrawStatEntries(statBlock, parent.occupiedSlots[SlotDefOf.Core]);
            }
            
        }

        //Gizmo Components
        public void Draw_GizmoSlot(SlotDef slot)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Vector2 position = positions[slot.uiPriority];
            Rect gizmoRect = new Rect(position,GizmoSize);
            if (slot.isCoreFrame) { 
                gizmoRect = gizmoRect.ScaledBy(2f); 
                gizmoRect.position = position;
            }
            
            bool disabled = !parent.slotDefs.Contains(slot);
            bool hasThing = parent.occupiedSlots.ContainsKey(slot);
            float healthPerc = 0f;
            //标签
            {
                TaggedString label = $"{slot.label.Translate()}";
                if (hasThing)
                {
                    Thing thing = parent.occupiedSlots[slot];
                    healthPerc = (float)thing.HitPoints / (float)thing.MaxHitPoints;
                    label += "(" + healthPerc.ToStringPercent() + ")";
                }
                else if (disabled)
                {
                    label += "("+"Disabled".Translate()+")";
                }
                Text.Font = GameFont.Small;
                Vector2 labelSize = Text.CalcSize(label);
                Rect labelBlock = new Rect(gizmoRect.x, gizmoRect.y - labelSize.y, gizmoRect.width, labelSize.y);
                Widgets.LabelFit(labelBlock, label);
            }
            //血条
            if (hasThing)
                GizmoHealthBar(gizmoRect, slot,healthPerc);
            //灰边
            {
                Widgets.DrawBoxSolid(gizmoRect, grey);
                gizmoRect = gizmoRect.ContractedBy(3f);
            }
            
            //底色
            {
                Material material = disabled ? TexUI.GrayscaleGUI : null;
                GenUI.DrawTextureWithMaterial(gizmoRect,Command.BGTex,material);
            }
            if(disabled) return;
            Texture2D icon = EmptySlotIcon;
            if (hasThing)
                icon = new CachedTexture(parent.occupiedSlots[slot].def.graphicData.texPath).Texture;
            GizmoInteraction(gizmoRect,icon,slot);
            Widgets.DrawHighlightIfMouseover(gizmoRect);
            //部件名字
            if (hasThing)
            {
                Rect nameBlock = gizmoRect.BottomPart(1f/8f);
                nameBlock.y -= nameBlock.height/4f;
                GUI.DrawTexture(nameBlock, TexUI.TextBGBlack);
                Widgets.LabelFit(nameBlock, parent.occupiedSlots[slot].LabelCap);
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }
        private void GizmoHealthBar(Rect gizmoRect,SlotDef slot,float healthPerc)
        {
            bool leftBar = slot.uiPriority == 0 || slot.uiPriority > 3;
            Rect bar = gizmoRect.LeftPart(1f/15f);
            if (leftBar)
            {
                bar.x = gizmoRect.x - 1.5f*bar.width;
            }
            else
            {
                bar.x = gizmoRect.xMax + 0.5f * bar.width;
            }
            Widgets.DrawBoxSolid(bar,Color.black);
            bar.y-=bar.height*(1f-healthPerc);
            bar.height*=healthPerc;
            Widgets.DrawBoxSolid(bar,Color.green);
        }
        private void GizmoInteraction(Rect rect, Texture2D icon,SlotDef slot)
        {
            if (Widgets.ButtonImage(rect, icon))//没做完
            {
                switch (Event.current.button)
                {
                    case 0://左键
                        {
                            if (parent.occupiedSlots.ContainsKey(slot))
                            {
                                Find.WindowStack.Add(new Dialog_InfoCard(parent.occupiedSlots[slot]));
                            }
                            else Find.WindowStack.Add(new Dialog_InfoCard(slot));
                            break;
                        }
                    case 1://右键
                        {
                            List<FloatMenuOption> options = new List<FloatMenuOption>();
                            if (parent.occupiedSlots.ContainsKey(slot))
                            {
                                Thing thing = parent.occupiedSlots[slot];
                                string label = "Remove".Translate() + thing.LabelCap;
                                Action action = () => parent.RemoveComp(thing);
                                options.Add(new FloatMenuOption(label, action, MenuOptionPriority.High));
                            }
                            options.AddRange(TryMakeOptions(slot));
                            if (!options.Empty())
                                Find.WindowStack.Add(new FloatMenu(options));
                            break;
                        }
                }
            }
        }
        private IEnumerable<FloatMenuOption> TryMakeOptions(SlotDef slot)
        {
            parent.UpdateCache();
            if (Building_MaintenanceBay.availableCompsForSlots.ContainsKey(slot))
            {
                foreach (var thing in Building_MaintenanceBay.availableCompsForSlots[slot])
                {
                    string label = thing.LabelCap;
                    Action action = () => parent.AddOrReplaceComp(thing);
                    yield return new FloatMenuOption(label,action);
                }
            }

            yield break;
        }
        //Stats Components
        private void DrawStatEntries(Rect rect,Thing thing)
        {
            WidgetRow row = new WidgetRow(rect.x,rect.y,UIDirection.RightThenDown,rect.width);
            float curY;
            row.Label("Performance".Translate());
            row.Gap(int.MaxValue);
            //replace
            row.Gap(int.MaxValue);
            row.Gap(int.MaxValue);
            row.Gap(int.MaxValue);
            row.Label("OverallArmor".Translate());
            curY = row.FinalY;            
            foreach (StatDef statDef in toDraw)
            {
                float statValue = thing.GetStatValue(statDef, true, -1);
                if (statDef.showOnDefaultValue || statValue != statDef.defaultBaseValue)
                {
                    curY += new StatDrawEntry(statDef.category, statDef, statValue, StatRequest.For(thing), ToStringNumberSense.Undefined, null, false).
                        Draw(rect.x,curY,rect.width,false,false,false,null,null,new Vector2(),new Rect());
                     
                }
            }
            Widgets.EndGroup();
        }
        

        private static readonly  Dictionary<int,Vector2> positions =new Dictionary<int, Vector2>()
        {
            {0,new Vector2(170f,56f)},
            {1,new Vector2(14f,56f)},
            {2,new Vector2(14f,164f)},
            {3,new Vector2(14f,282f)},
            {4,new Vector2(412f,56f)},
            {5,new Vector2(412f,164f)},
            {6,new Vector2(412f,282f)},
        };
        private static readonly List<StatDef> toDraw = new List<StatDef>() {
            StatDefOf.ArmorRating_Sharp, StatDefOf.ArmorRating_Blunt,StatDefOf.ArmorRating_Heat
        };
    }
}
