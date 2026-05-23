namespace RaizesNordesteWeb.API.DTOs
{

    public class ErroPadrao
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<DetalheErro> Details { get; set; } = new();
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
        public string Path { get; set; } = string.Empty;
        public string? RequestId { get; set; }

        public static ErroPadrao Criar(string error, string message, string path,
            List<DetalheErro>? details = null, string? requestId = null)
        {
            return new ErroPadrao
            {
                Error = error,
                Message = message,
                Path = path,
                Details = details ?? new(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                RequestId = requestId
            };
        }
    }

    public class DetalheErro
    {
        public string Field { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
    }
}
