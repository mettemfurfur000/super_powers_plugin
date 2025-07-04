using super_powers_plugin.src;

public enum PowerRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
};


public class ShopPower
{
    private PowerRarity rarity = 0;
    private uint price = 0;
    private bool noShop = false;

    public PowerRarity Rarity { get => rarity; set => rarity = value; }         // used by a shop power
    public uint Price { get => price; set => price = value; }            // used by a shop power
    public bool NoShop { get => noShop; set => noShop = value; }        // used by a shop power
}