namespace AwsTranscribe.Models
{
    class AWSJob
    {
        public string JobName { get; set; }
        public string AccountId { get; set; }
        public AWSJobResult Results { get; set; }

        public string Status { get; set; }
    }
}
