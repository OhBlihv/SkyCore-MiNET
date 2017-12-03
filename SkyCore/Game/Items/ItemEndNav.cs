using fNbt;using MiNET.Items;namespace SkyCore.Game.Items
{
    class ItemEndNav : ItemCompass
    {

        private NbtCompound _extraData;

        public override NbtCompound ExtraData
        {
            get => _extraData = UpdateExtraData();
            set => _extraData = value;
        }

        private NbtCompound UpdateExtraData()
        {
            if (_extraData == null)
            {
                _extraData = new NbtCompound
                {
                    new NbtCompound("display")
                    {
                        new NbtString("Name", "§r§d§lHold to Reopen Modal§r")
                    }
                };
            }
            
            return _extraData;
        }


    }
}
