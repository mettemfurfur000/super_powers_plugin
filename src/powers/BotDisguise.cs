using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace super_powers_plugin.src;

public class BotDisguise : ISuperPower
{
    public BotDisguise()
    {
        Triggers = [typeof(EventRoundStart)];
        setDisabled();
    } // TODO: clear player names from dropped weapons

    public override HookResult Execute(GameEvent gameEvent)
    {
        var e = gameEvent as EventRoundStart;

        Users.ForEach(u => ChangeNameRevertable(u));

        return HookResult.Continue;
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player == null)
        {
            Users.ForEach(u => RevertName(u));
            return;
        }

        RevertName(player);
    }

    public List<string> name_pool = ["Maddison", "Colton", "Rose", "Phoenix", "Maxine", "Chase", "Anna", "Andres", "Jaliyah", "Fox", "Emerie", "Karsyn", "Faye", "Lennox", "Reign", "Cole", "Kynlee", "Emory", "Bethany", "Van", "Emory", "Kenji", "Ivy", "Kane", "Alivia", "Bryce", "Milan", "Riley", "Reina", "Idris", "Ellis", "Nova", "Giovanna", "Ulises", "Harper", "Mark", "Mercy", "Iker", "Rowan", "Blake", "Mariah", "Korbin", "Nola", "Dillon", "Amara", "Gael", "Briana", "Dane", "Melany", "Quentin", "Sutton", "Shepherd", "Margo", "Matthias", "Paris", "Allen", "Whitney", "Blaze", "Leyla", "Eden", "Remy", "Remi", "Izabella", "Victor", "Freyja", "Waylon", "Judith", "Enoch", "Kinslee", "Marlon", "Jade", "Zyair", "Ryleigh", "Aaron", "Miracle", "Kannon", "Aaliyah", "Lochlan", "Ivanna", "Luka", "Kairi", "Jason", "Megan", "Kohen", "Bexley", "Patrick", "Persephone", "Shepard", "Ariella", "Johnathan", "Josephine", "Jacob", "Ansley", "Solomon", "Aylin", "Armando", "Aaliyah", "Anthony", "Kendra", "Jones", "Gracie", "Osiris", "Kylee", "Blaise", "Adeline", "Rodney", "Destiny", "Dominick", "Estelle", "Reuben", "Mia", "Cody", "Iyla", "Fabian", "Oakleigh", "Roger", "Anaya", "Brodie", "Emmalyn", "Memphis", "Keily", "Forest", "Millie", "Jorge", "Elise", "Caleb", "Summer", "Manuel", "Pearl", "Pierce", "Rosalia", "Edgar", "June", "Marley", "Marlowe", "Edgar", "Mavis", "Kashton", "Dayana", "Marshall", "Alanna", "Layne", "Adelina", "Mekhi"];

    public override string GetDescription() => $"Disguise as a bot (to a certain point)";

    public override void Update()
    {
        if (Server.TickCount % 128 == 0)
            Users.ForEach(u => TemUtils.CleanWeaponOwner(u));
        if (Server.TickCount % 1024 == 0)
            Users.ForEach(u => ChangeNameRevertable(u));
    }

    private void ChangeNameRevertable(CCSPlayerController player)
    {
        if (player.IsBot)
            return;

        ulong uuid = player.SteamID;

        if (!originalNames.ContainsKey(uuid))
        {
            originalNames[uuid] = player.PlayerName;
        }

        if (chosenNames.ContainsKey(uuid))
        {
            TemUtils.UpdatePlayerName(player, chosenNames[uuid], "BOT");
            return;
        }

        ulong nameIndex = (ulong)Random.Shared.Next() % (ulong)name_pool.Count; // comp an index from da current name
        string name = name_pool[(int)nameIndex];

        chosenNames[uuid] = name;

        TemUtils.UpdatePlayerName(player, name, "BOT");
    }

    private void RevertName(CCSPlayerController player)
    {
        if (player.IsBot)
            return;

        ulong uuid = player.SteamID;

        if (originalNames.ContainsKey(uuid))
        {
            TemUtils.UpdatePlayerName(player, originalNames[uuid]);

            originalNames.Remove(uuid);
            chosenNames.Remove(uuid);
        }
    }

    public Dictionary<ulong, string> originalNames = [];
    public Dictionary<ulong, string> chosenNames = [];
}

