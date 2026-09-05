using System.Reflection;
using System.Text.RegularExpressions;
using super_powers_plugin.src;

namespace super_powers_plugin.src;

public static class ModifierConfigHelper
{
    public static BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static readonly string Prefix = "cfg_";

    public static IEnumerable<T> SelectByName<T>(IEnumerable<T> items, string pattern, Func<T, string> getName)
    {
        string r_pattern = TemUtils.WildCardToRegular(pattern);
        return items.Where(item => Regex.IsMatch(getName(item), r_pattern));
    }

    public static void GenerateAddFields(Dictionary<string, string> dest, object instance, Type type)
    {
        var iter = type;

        do
        {
            var fields = iter.GetFields(FieldFlags);

            foreach (var property in fields)
            {
                var property_name = property.Name;
                if (!property_name.StartsWith(Prefix))
                    continue;
                property_name = property_name.Replace(Prefix, "");

                var property_value = property.GetValue(instance);

                if (property_value != null && !dest.ContainsKey(property_name))
                    dest.Add(property_name, property_value.ToString() ?? "null");
            }

            iter = iter.BaseType;
        } while (iter != null);
    }

    public static void ParseConfig(object target, Type iter_type, Dictionary<string, string> cfg_unresolved)
    {
        if (cfg_unresolved.Count == 0)
            return;

        Dictionary<string, string> next_unresolved = [];

        foreach (var field in cfg_unresolved)
        {
            string actualKey = Prefix + field.Key;
            var fieldInfo = iter_type.GetField(actualKey, FieldFlags);

            if (fieldInfo == null)
            {
                next_unresolved.Add(field.Key, field.Value);
                continue;
            }

            try
            {
                try
                {
                    fieldInfo.SetValue(target, Convert.ChangeType(field.Value, fieldInfo.FieldType));
                }
                catch (InvalidCastException ex) { TemUtils.AlertError($"Error occured while processing {iter_type} : Failed to convert value for {fieldInfo.Name}: {ex.Message}"); }
                catch (FormatException ex) { TemUtils.AlertError($"Error occured while processing {iter_type} : Invalid format for {fieldInfo.Name}: {ex.Message}"); }
            }
            catch (Exception ex)
            {
                TemUtils.AlertError($"Error occured while processing {iter_type} : Invalid format for {fieldInfo.Name}: {ex.Message}");
            }
        }

        if (iter_type.BaseType != null)
            ParseConfig(target, iter_type.BaseType, next_unresolved);
        else if (next_unresolved.SequenceEqual(cfg_unresolved))
        {
            TemUtils.AlertError("Failed to resolve some fields for '" + iter_type + "', list of them:");
            foreach (var field in next_unresolved)
                TemUtils.AlertError(field.Key + " : " + field.Value);
            return;
        }
    }

    public static void FeedConfig<T>(
        Dictionary<string, Dictionary<string, string>> cfgEntries,
        IEnumerable<T> items,
        Func<T, string> getName,
        Action<T, Dictionary<string, string>> parseCfg)
    {
        var defaults = GenerateDefaultConfig(items, getName);

        foreach (var (itemName, defaultFields) in defaults)
        {
            if (cfgEntries.TryGetValue(itemName, out var existingFields))
            {
                foreach (var (key, value) in defaultFields)
                    if (!existingFields.ContainsKey(key))
                        existingFields[key] = value;
            }
            else
            {
                cfgEntries[itemName] = new Dictionary<string, string>(defaultFields);
            }
        }

        var stale = cfgEntries.Keys.Where(k => !defaults.ContainsKey(k)).ToList();
        foreach (var key in stale)
            cfgEntries.Remove(key);

        foreach (var item in items)
        {
            if (cfgEntries.TryGetValue(getName(item), out var itemCfg))
                parseCfg(item, itemCfg);
        }
    }

    public static Dictionary<string, Dictionary<string, string>> GenerateDefaultConfig<T>(
        IEnumerable<T> items,
        Func<T, string> getName)
    {
        Dictionary<string, Dictionary<string, string>> args = [];

        foreach (var item in items)
        {
            if (item == null)
                continue;

            var itemName = getName(item);
            if (itemName == null) continue;

            args.Add(itemName, []);
            GenerateAddFields(args[itemName], item, item.GetType());
        }

        return args;
    }

    public static string? InspectReflective(object target, Type type)
    {
        string output = "";
        var fields = type.GetFields(FieldFlags);
        int total = 0;

        foreach (var field in fields)
        {
            var fieldInfo = type.GetField(field.Name, FieldFlags);
            var property_name = field.Name;

            if (!property_name.StartsWith(Prefix))
                continue;
            property_name = property_name.Replace(Prefix, "");

            if (fieldInfo != null)
            {
                output += $"{property_name}: {fieldInfo.GetValue(target)}\n";
                total++;
            }
            else
                output += $"{property_name}: null\n";
        }

        if (total == 0)
            return null;

        return output;
    }

    public static void Reconfigure<T>(
        Dictionary<string, string> configuration,
        string namePattern,
        IEnumerable<T> items,
        Func<T, string> getName,
        Action<T, Dictionary<string, string>> parseCfg)
    {
        var matched = SelectByName(items, namePattern, getName);
        foreach (var item in matched)
            parseCfg(item, configuration);
    }
}
