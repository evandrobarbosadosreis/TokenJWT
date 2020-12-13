namespace webapi.Services.Configurations
{
    public sealed class TokenConfiguration
    {
        public string Audience { get; }
        public string Issuer { get; }
        public string Secret { get; }
        public int MinutesToExpire { get; }
        public int DaysToRefresh { get; }
    }
}