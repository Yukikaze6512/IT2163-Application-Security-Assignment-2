using System.Text.Json.Serialization;

namespace ApplicationSecurity.Services
{
    public class ReCaptchaService
    {
        private readonly string _secretKey;
        private readonly HttpClient _httpClient;

        public ReCaptchaService(IConfiguration configuration, HttpClient httpClient)
        {
            _secretKey = configuration["GoogleReCaptcha:SecretKey"] ?? string.Empty;
            _httpClient = httpClient;
        }

        public async Task<bool> VerifyToken(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var response = await _httpClient.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}",
                    null);

                var result = await response.Content.ReadFromJsonAsync<ReCaptchaResponse>();
                return result?.Success == true && result.Score >= 0.5;
            }
            catch
            {
                // If reCAPTCHA service is unavailable, allow the request
                // In production, you may want to deny instead
                return true;
            }
        }
    }

    public class ReCaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public float Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
