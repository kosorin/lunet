namespace Lure
{
    public static class Time
    {
        public const int MillisecondsPerSecond = 1000;
        public const int MillisecondsPerMinute = MillisecondsPerSecond * SecondsPerMinute;
        public const int MillisecondsPerHour = MillisecondsPerMinute * MinutesPerHour;
        public const int MillisecondsPerDay = MillisecondsPerHour * HoursPerDay;
        public const int MillisecondsPerWeek = MillisecondsPerDay * DaysPerWeek;

        public const int SecondsPerMinute = 60;
        public const int SecondsPerHour = SecondsPerMinute * MinutesPerHour;
        public const int SecondsPerDay = SecondsPerHour * HoursPerDay;
        public const int SecondsPerWeek = SecondsPerDay * DaysPerWeek;

        public const int MinutesPerHour = 60;
        public const int MinutesPerDay = MinutesPerHour * HoursPerDay;
        public const int MinutesPerWeek = MinutesPerDay * DaysPerWeek;

        public const int HoursPerDay = 24;
        public const int HoursPerWeek = HoursPerDay * DaysPerWeek;

        public const int DaysPerWeek = 7;
    }
}
