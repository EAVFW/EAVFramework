namespace EAVFramework.Events
{
    public static class EventIds
    {
        //////////////////////////////////////////////////////
        /// Error related events
        //////////////////////////////////////////////////////
        private const int ErrorEventsStart = 3000;

        public const int UnhandledException = ErrorEventsStart + 0;
        public const int InvalidClientConfiguration = ErrorEventsStart + 1;
    }
}
