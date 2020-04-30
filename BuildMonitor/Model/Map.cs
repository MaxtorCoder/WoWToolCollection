namespace BuildMonitor.Model
{
    public class Map
    {
        public int Id;
        public string Directory;
        public string MapName;
        public string Field901_002;
        public string MapDescription0;
        public string MapDescription1;
        public string PvpShortDescription;
        public string PvpLongDescription;
        public float[] Corpse = new float[2];
        public byte MapType;
        public byte InstanceType;
        public byte ExpansionId;
        public ushort AreaTableId;
        public short LoadingScreenId;
        public short TimeOfDayOverride;
        public short ParentMapId;
        public short CosmeticParentMapId;
        public byte TimeOffset;
        public float MinimapIconScale;
        public short CorpseMapId;
        public byte MaxPlayers;
        public short WindSettingsId;
        public int ZmpFileDataId;
        public uint WdtFileDataId;
        public int[] Flags = new int[2];
    }
}
