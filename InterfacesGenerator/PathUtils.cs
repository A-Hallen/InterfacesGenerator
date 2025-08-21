using System.Text;

namespace GeneradorInterfaces;

public static class PathUtils
{
    public static string GetRelativePath(string from, string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        {
            return ".";
        }

        var fromParts = from.Split('/', '\\');
        var toParts = to.Split('/', '\\');

        var commonParts = 0;
        for (int i = 0; i < Math.Min(fromParts.Length, toParts.Length); i++)
        {
            if (fromParts[i].Equals(toParts[i], StringComparison.OrdinalIgnoreCase))
            {
                commonParts++;
            }
            else
            {
                break;
            }
        }

        var result = new StringBuilder();

        for (int i = commonParts; i < fromParts.Length; i++)
        {
            result.Append("../");
        }

        for (int i = commonParts; i < toParts.Length; i++)
        {
            result.Append(toParts[i]);
            if (i < toParts.Length - 1)
            {
                result.Append("/");
            }
        }

        return result.Length == 0 ? "." : result.ToString();
    }
}
