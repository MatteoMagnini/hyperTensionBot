namespace HyperTensionBot.Server.Bot.Extensions {
    public static class Time {

        // conversion UTC time to local time
        // Default ita, it can be changed to any time zone 
        public static DateTime Convert(DateTime date, string timeZoneId = "Europe/Rome") {

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            return TimeZoneInfo.ConvertTimeFromUtc(date, timeZone);
        }
    }
}
