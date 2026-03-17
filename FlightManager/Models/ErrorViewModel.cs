namespace FlightManager.Models
{
    public class ErrorViewModel
    {
        public string Code { get; set; }

        public string Description { get; set; }

        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public ErrorViewModel()
        {

        }

        public ErrorViewModel(string code, string description, string requestId = null)
        {
            Code = code;
            Description = description;
            RequestId = requestId;
        }
    }
}
