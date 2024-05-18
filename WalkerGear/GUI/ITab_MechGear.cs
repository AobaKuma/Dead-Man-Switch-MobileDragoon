using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;


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
        private Building_MaintenanceBay Parent
        {
            get => SelThing as Building_MaintenanceBay;
        }
        private const float side = 80f;
        private Vector2 GizmoSize
        {
            get => new(side, side);
        }
        private readonly Color grey = new ColorInt(72, 82, 92).ToColor;

        private static readonly Texture2D EmptySlotIcon = Command.BGTex;

        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            Rect rect = new(0f, 0f, this.size.x, this.size.y);
            Rect inner = rect.ContractedBy(3f);
            //Draw Title
            {
                Text.Anchor = TextAnchor.UpperRight;
                string title = "MechSolution".Translate();
                Vector2 titleSize = Text.CalcSize(title);
                Rect titleBox = new(new Vector2(inner.xMax - titleSize.x - 20f, inner.y), titleSize);
                Widgets.LabelFit(titleBox, title);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            //Draw S/L solution
            {
                Vector2 slPosition = new(14f, inner.y);

                string text = "Save".Translate();
                Vector2 size = Text.CalcSize(text);
                size.Scale(new(1.2f,1.2f));
                Rect slgizmoRect = new(slPosition, size);
                Widgets.ButtonText(slgizmoRect,text);
                text = "Load".Translate();
                slgizmoRect.x = slgizmoRect.xMax + 10f;
                size=Text.CalcSize(text);
                size.Scale(new(1.2f, 1.2f));
                slgizmoRect.size = size;
                Widgets.ButtonText(slgizmoRect, text);
                text = "UnArm".Translate();
                slgizmoRect.x = slgizmoRect.xMax + 10f;
                size = Text.CalcSize(text);
                size.Scale(new(1.2f, 1.2f));
                slgizmoRect.size = size;
                Widgets.ButtonText(slgizmoRect, text);
            }

            foreach (SlotDef slot in Parent.slotDefs)
            {
                Draw_GizmoSlot(slot);
            }
            if (Parent.HasGearCore)
            {
                Vector2 position = positions[0];
                //stats
                {position.x =170f - (side / 5f);
                position.y = 56f+(side * 2 + 5f);
                Vector2 box = GizmoSize * 2f;
                box.x *= 1.1f;
                box.y = size.y - position.y - 10f;
                Rect statBlock = new(position, box);
                    DrawStatEntries(statBlock, Parent.occupiedSlots[SlotDefOf.Core]);
                }

                //rotate&color
                Vector2 rotateGizmosBotLeft = new(340f, 216f);
                {
                    Vector2 smallGizmoSize = new(30f,30f);
                    
                    for (int i = 0; i < 3; i++)
                    {
                        rotateGizmosBotLeft.y -= 30f;
                        Rect gizmoRect = new(rotateGizmosBotLeft,smallGizmoSize);
                        switch (i)
                        {
                            case 0://染色
                                
                                break;
                            case 1:
                                if (Widgets.ButtonImage(gizmoRect, Building_MaintenanceBay.rotateButton))
                                    Parent.direction.Rotate(RotationDirection.Clockwise);
                                break;
                            case 2:

                                if (Widgets.ButtonImage(gizmoRect, Building_MaintenanceBay.rotateOppoButton))
                                    Parent.direction.Rotate(RotationDirection.Counterclockwise);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            
        }

        //Gizmo Components
        public void Draw_GizmoSlot(SlotDef slot)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Vector2 position = positions[slot.uiPriority];
            Rect gizmoRect = new(position, GizmoSize);
            if (slot.isCoreFrame) { 
                gizmoRect = gizmoRect.ScaledBy(2f); 
                gizmoRect.position = position;
            }
            var disabledSlots = Parent.occupiedSlots?.GetValueOrDefault(SlotDefOf.Core)
                ?.TryGetComp<CompWalkerComponent>().Props.ItemDef?.GetCompProperties<CompProperties_WalkerComponent>()?.disabledSlots;
            bool disabled = disabledSlots!=null&&disabledSlots.Contains(slot);
            bool hasThing = Parent.occupiedSlots.ContainsKey(slot);
            float healthPerc = 0f;
            //标签
            {
                TaggedString label = $"{slot.label.Translate()}";
                if (hasThing)
                {
                    Thing thing = Parent.occupiedSlots[slot];
                    healthPerc = (float)thing.HitPoints / (float)thing.MaxHitPoints;
                    label += "(" + healthPerc.ToStringPercent() + ")";
                }
                else if (disabled)
                {
                    label += "("+"Disabled".Translate()+")";
                }
                Text.Font = GameFont.Small;
                Vector2 labelSize = Text.CalcSize(label);
                Rect labelBlock = new(gizmoRect.x, gizmoRect.y - labelSize.y, gizmoRect.width, labelSize.y);
                Widgets.LabelFit(labelBlock, label);
            }
            //血条
            if (hasThing)
                GizmoHealthBar(gizmoRect, slot, Parent.occupiedSlots[slot]);
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
                icon = new CachedTexture(Parent.occupiedSlots[slot].def.graphicData.texPath).Texture;
            GizmoInteraction(gizmoRect,icon,slot);
            Widgets.DrawHighlightIfMouseover(gizmoRect);
            //部件名字
            if (hasThing)
            {
                Rect nameBlock = gizmoRect.BottomPart(1f/8f);
                nameBlock.y+=nameBlock.height-26f;
                nameBlock.height = 26f;
                GUI.DrawTexture(nameBlock, TexUI.TextBGBlack);
                Widgets.LabelFit(nameBlock, Parent.occupiedSlots[slot].LabelCap);
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        private void GizmoHealthBar(Rect gizmoRect,SlotDef slot,Thing t)
        {
            var healthPerc = (float)t.HitPoints/t.MaxHitPoints;
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
            if (slot.isCoreFrame && Parent.HasGearCore)
            {
                RenderTexture portrait = PortraitsCache.Get(Parent.Dummy, rect.size, Parent.direction);
                Widgets.DrawTextureFitted(rect,portrait,1f);
            }
            else Widgets.DrawTextureFitted(rect, icon, 1f);

            if (Widgets.ButtonInvisible(rect, icon))//没做完
            {
                switch (Event.current.button)
                {
                    case 0://左键
                        {
                            if (Parent.occupiedSlots.ContainsKey(slot))
                            {
                                Find.WindowStack.Add(new Dialog_InfoCard(Parent.occupiedSlots[slot]));
                            }
                            else Find.WindowStack.Add(new Dialog_InfoCard(slot));
                            break;
                        }
                    case 1://右键
                        {
                            List<FloatMenuOption> options = new();
                            if (Parent.occupiedSlots.ContainsKey(slot))
                            {
                                Thing thing = Parent.occupiedSlots[slot];
                                string label = "Remove".Translate() + thing.LabelCap;
                                Action action = () => Parent.RemoveComp(thing);
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
            Parent.UpdateCache();
            if (Parent.availableCompsForSlots.ContainsKey(slot))
            {
                foreach (var thing in Parent.availableCompsForSlots[slot])
                {
                    string label = thing.LabelCap;
                    Action action = () => Parent.AddOrReplaceComp(thing);
                    yield return new FloatMenuOption(label,action);
                }
            }
        }
        //Stats Components
        private void DrawStatEntries(Rect rect,Thing thing)
        {
            WidgetRow row = new(rect.x,rect.y,UIDirection.RightThenDown,rect.width);
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
        }

        private static readonly Dictionary<int, Vector2> positions = new()
        {
            {0,new(170f,56f)},
            {1,new(14f,56f)},
            {2,new(14f,164f)},
            {3,new(14f,282f)},
            {4,new(412f,56f)},
            {5,new(412f,164f)},
            {6,new(412f,282f)},
        };
        private static readonly List<StatDef> toDraw = new() {
            StatDefOf.ArmorRating_Sharp, StatDefOf.ArmorRating_Blunt,StatDefOf.ArmorRating_Heat
        };
    }
}
