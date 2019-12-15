using Newtonsoft.Json;
using Sensus.UI.UiProperties;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sensus.Anonymization.Anonymizers
{
	public class RegExAnonymizer : Anonymizer
	{
		[JsonIgnore]
		public string DEFAULT_REPLACEMENT = "\u2588";

		public override string DisplayText => "RegEx";

		[EntryStringUiProperty("Pattern:", true, 1, true)]
		public string Pattern { get; set; }
		[EntryStringUiProperty("Replacement:", true, 1, true)]
		public string Replacement { get; set; }

		public RegExAnonymizer()
		{
			Replacement = DEFAULT_REPLACEMENT;
		}

		public override object Apply(object value, Protocol protocol)
		{
			if (string.IsNullOrEmpty(Pattern) == false && value is string stringValue)
			{
				return Regex.Replace(stringValue, Pattern, m =>
				{
					return string.Join("", Enumerable.Repeat(Replacement, m.Value.Length));
				});
			}

			return value;
		}

		public override bool Validate(out string errorMessage)
		{
			errorMessage = null;

			if (string.IsNullOrEmpty(Pattern))
			{
				errorMessage = "Pattern is required.";

				return false;
			}

			try
			{
				Regex regex = new Regex(Pattern);
			}
			catch (Exception error)
			{
				errorMessage = error.Message;

				return false;
			}

			return true;
		}
	}
}
