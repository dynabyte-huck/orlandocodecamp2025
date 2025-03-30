namespace AspireDaprDemo.ServiceDefaults;

public static class GlobalConstants
{
    public const string PubSubName = "demopubsub";

    public const string SecretStoreName = "demosecretstore";

    public const string SmtpBindingName = "demosmtpbinding";

    public const string StateStoreName = "demostore";

    public static class AppIds
    {
        public const string NotificationService = "demonotsvc";
        public const string SubscriberService = "demosubsvc";
        public const string WeatherService = "demowthrsvc";
        public const string WorkflowService = "demowrkflwsvc";
    }

    public static class EventTopics
    {
        public const string NotifyEmailTopicName = "notifyemail";
    }
}