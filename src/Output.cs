namespace OidcCli;

internal class Output
{
    public string? Error { get; set; }

    public IEnumerable<Claim>? Claims { get; set; }

    public string? IdToken { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
        
    public class Claim
    {
        public string Type { get; set; } = null!;

        public string Value { get; set; } = null!;
    }
}