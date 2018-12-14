using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Extensions
{
    public static class JsonConvertExtensions
    {
        public static bool IsValidJson(this string strInput)
        {
            var rVal = false;
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    rVal = true;
                }
                catch (Exception ex) { }
            }
            return rVal;
        }
    }
}
