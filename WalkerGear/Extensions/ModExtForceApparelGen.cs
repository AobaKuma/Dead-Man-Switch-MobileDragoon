
using System.Collections.Generic;
using Verse;

namespace WalkerGear;


public class ModExtForceApparelGen : DefModExtension
{
    public List<ThingDef> apparels;
    public FloatRange StructurePointRange = new FloatRange(1, 1);
}