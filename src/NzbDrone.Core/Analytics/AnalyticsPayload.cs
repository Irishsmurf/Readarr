namespace NzbDrone.Core.Analytics
{
    // The entire payload sent to POST /analytics - the exact field allow-list from
    // docs/analytics.md (Readarr repo). Serialized camelCase by the app's global
    // Json settings, matching docs/ingest-endpoint.md §2.1's wire contract.
    public class AnalyticsPayload
    {
        public string Version { get; set; }
        public string Branch { get; set; }
        public string Os { get; set; }
        public string Arch { get; set; }
        public string RuntimeVersion { get; set; }
        public string DbType { get; set; }
        public bool UsingCustomMetadataSource { get; set; }
    }
}
