using System;

namespace Sensus.Tools.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime Max(this DateTime d1, DateTime d2)
        {
            return d1 > d2 ? d1 : d2;
        }

        public static DateTime Max(this DateTime? d1, DateTime d2)
        {
            return d1 > d2 ? d1.Value : d2;
        }

        public static DateTime Max(this DateTime d1, DateTime? d2)
        {
            return d2 > d1 ? d2.Value : d1;
        }
    }
}