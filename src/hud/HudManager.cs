namespace super_powers_plugin.src.hud;

public static class SuperPowerHudManager
{
    public static string BuildBar(float progress, int width = 16)
    {
        progress = Math.Clamp(progress, 0f, 1f);
        int filled = (int)(progress * width);

        var sb = new System.Text.StringBuilder(width + 4);
        sb.Append('[');
        for (int i = 0; i < width; i++)
            sb.Append(i < filled ? '\u2588' : '\u2591');
        sb.Append(']');
        sb.Append($" {progress * 100:F0}%");
        return sb.ToString();
    }
}