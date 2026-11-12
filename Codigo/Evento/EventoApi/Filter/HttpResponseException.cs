namespace EventoApi.Filter
{
    public class HttpResponseException : Exception
    {
        public int Status { get; set; } = 400;

        public object? Value { get; set; }

        public HttpResponseException(string value)
        {
            this.Value = value;
        }

        public HttpResponseException(int status, object? value)
        {
            this.Status = status;
            this.Value = value;
        }
    }
}

