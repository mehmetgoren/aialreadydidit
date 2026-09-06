using AiAlreadyDidIt.Api.Infrastructure;

namespace AiAlreadyDidIt.Tests;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_verify_round_trips()
    {
        var hash = _hasher.Hash("Aadi123!");
        Assert.StartsWith("pbkdf2$210000$", hash);
        Assert.True(_hasher.Verify("Aadi123!", hash));
        Assert.False(_hasher.Verify("aadi123!", hash));
        Assert.False(_hasher.Verify("", hash));
    }

    [Fact]
    public void Hash_is_salted_so_same_password_differs()
    {
        Assert.NotEqual(_hasher.Hash("secret"), _hasher.Hash("secret"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain")]
    [InlineData("bcrypt$10$abc$def")]
    [InlineData("pbkdf2$notanumber$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2$1000$c2FsdA==")]
    public void Verify_rejects_malformed_hashes(string hash)
    {
        Assert.False(_hasher.Verify("secret", hash));
    }
}
