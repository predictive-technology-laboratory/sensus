using System.Linq;
using System.Text.RegularExpressions;

namespace Sensus.UI.Inputs
{
	public static class ButtonValueParser
	{
		public static (string, string, bool, bool) ParseButtonValue(string value, bool split)
		{
			bool isOther = false;
			bool isExclusive = false;

			value = Regex.Replace(value, "^((?<other>!)|(?<exclusive>\\^))+", m =>
			{
				if (m.Groups["other"].Success)
				{
					isOther = true;
				}
				
				if (m.Groups["exclusive"].Success)
				{
					isExclusive = true;
				}
				
				return "";
			});

			value = Regex.Replace(value, "{(?<escaped>(!|\\^))}", m =>
			{
				return m.Groups["escaped"].Value;
			});

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

			return (text, value, isOther, isExclusive);
		}
	}
}
