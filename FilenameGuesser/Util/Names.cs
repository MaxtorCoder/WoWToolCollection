namespace FilenameGuesser.Util
{
    public static class Names
    {
        public static string GetPathFromName(string modelName)
        {
            var newmodelname = modelName.Split('_');
            switch (newmodelname[0].ToLower())
            {
                case "9bo":
                    return "world/expansion08/doodads/broker";
                case "9mal":
                    return "world/expansion08/doodads/maldraxxus";
                case "9cas":
                    return "world/expansion08/doodads/castlezone";
                case "9nc":
                    return "world/expansion08/doodads/necro";
                case "9ori":
                case "9ob":
                    return "world/expansion08/doodads/oribos";
                case "9ard":
                    return "world/expansion08/doodads/ardenweald";
                case "9mw":
                case "9maw":
                    return "world/expansion08/doodads/maw";
                case "9du":
                    return "world/expansion08/doodads/dungeon";
                case "9fx":
                    return "world/expansion08/doodads/fx";
                case "9fa":
                    return "world/expansion08/doodads/fae";
                case "9vm":
                    return "world/expansion08/doodads/vampire";
                case "9vl":
                    return "world/expansion08/doodads/valkyr";
                case "9xp":
                    return "world/expansion08/doodads";
                case "9pln":
                    return "world/expansion08/doodads/babylonzone";
                case "mage":
                case "cfx":
                    return "spells";
                case "polearm":
                case "staff":
                case "sword":
                case "mace":
                case "bow":
                case "crossbow":
                case "hand":
                case "axe":
                case "knife":
                case "firearm":
                case "glaive":
                case "offhand":
                case "wand":
                    return "item/objectcomponents/weapon";
                case "shield":
                case "buckler":
                    return "item/objectcomponents/shield";
                case "helm":
                    return "item/objectcomponents/head";
                case "cape":
                    return "item/objectcomponents/cape";
                case "shoulder":
                    return "item/objectcomponents/shoulder";
                case "bullet":
                    return "item/objectcomponents/ammo";
                case "buckle":
                    return "item/objectcomponents/waist";
                case "collections":
                    return "item/objectcomponents/collections";
                default:
                    return $"creature/{modelName}";
            }
        }
    }
}
