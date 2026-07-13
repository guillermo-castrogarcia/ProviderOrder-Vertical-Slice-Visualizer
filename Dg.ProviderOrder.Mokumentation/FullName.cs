namespace Dg.ProviderOrder.Mokumentation;

using System.Text.Json.Serialization;
using System.Web;

public sealed record FullName(string Value, string[] Tokens)
{
    public static FullName CreatEmpty() => new(string.Empty, []);
    public int TokenCount => Tokens.Length;

    public string LastToken => Tokens.Last();

    public static FullName FromValue(string value)
    {
        var tokens = value.Split(".", StringSplitOptions.RemoveEmptyEntries);
        return new FullName(value, tokens);
    }

    public static FullName FromTokens(IEnumerable<string> tokens)
    {
        var tokensArray = tokens.ToArray();
        var value = string.Join( ".", tokensArray);
        return new FullName(value, tokensArray);
    }

    public FullName GetPartialFullName(int tokenCount) => FullName.FromTokens(Tokens.Take(tokenCount));

    public FullName Intersect(FullName other) => FromTokens(Tokens.Intersect(other.Tokens));
    public FullName Except(FullName other) => FromTokens(Tokens.Except(other.Tokens));

    public bool Equals(FullName? other) => Value.Equals(other?.Value);

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString() => Value;

    [JsonIgnore]
    public string HtmlEncodedValue => HttpUtility.HtmlEncode(Value).Replace(";",string.Empty).Replace(" ", "&#160");

    [JsonIgnore]
    public FullName OnlyProductNameTokens => FromTokens(Tokens.SkipWhile(IsNotProductName));


    private static bool IsNotProductName(string token)
    {
        return token.Equals("dg", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("providerorder", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("application", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("infrastructure", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("framework", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("ProviderOrderInterface", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("KubernetesJobs", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("Commands", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("Queries", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("domain", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("export", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("import", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("CategoryManagement", StringComparison.InvariantCultureIgnoreCase)
            || token.Equals("OrderingProcess", StringComparison.InvariantCultureIgnoreCase);
    }
}