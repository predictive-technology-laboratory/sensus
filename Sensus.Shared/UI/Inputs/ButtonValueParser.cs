using System.Linq;
using System.Text.RegularExpressions;

namespace Sensus.UI.Inputs
{
	public static class ButtonValueParser
	{
		public static (string, string, bool) ParseButtonValue(string value, bool split)
		{
			bool isOther = false;

			if (value.StartsWith("!"))
			{
				value = Regex.Replace(value, @"^!", "");

				isOther = true;
			}
			else if (value.StartsWith("{!}"))
			{
				value = Regex.Replace(value, @"^{!}", "!");
			}

			string text = value;

			if (split)
			{
				string[] pair = Regex.Split(value, @"(?<=[^{])::(?=[^}])")
					.Select(x => Regex.Replace(x, "{::}", "::"))
					.ToArray();

				if (pair.Length > 2)
				{
					pair[1] = string.Join("::", pair.Skip(1));
				}

				value = pair.FirstOrDefault();
				text = pair.LastOrDefault();
			}

			return (text, value, isOther);
		}
	}
}
