using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Texture2D GetSafetyIcon_Disabled = ContentFinder<Texture2D>.Get("UI/Safety_Disabled");
        public static readonly Texture2D GetSafetyIcon = ContentFinder<Texture2D>.Get("UI/Safety");
        public static readonly Texture2D GetOutIcon = ContentFinder<Texture2D>.Get("UI/GetOffWalker");
        public static readonly Texture2D rotateButton = ContentFinder<Texture2D>.Get("UI/Rotate");
        public static readonly Texture2D rotateOppoButton = ContentFinder<Texture2D>.Get("UI/RotateOppo");
    }
}
