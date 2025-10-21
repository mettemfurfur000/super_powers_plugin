using super_powers_plugin.src;

public class ShopPower
{
    private string rarity = "Common";
    private uint price = 0;
    private bool noShop = false;

    public string Rarity { get => rarity; set => rarity = value; }         // used by a shop power
    public uint Price { get => price; set => price = value; }            // used by a shop power
    public bool NoShop { get => noShop; set => noShop = value; }        // used by a shop power
}