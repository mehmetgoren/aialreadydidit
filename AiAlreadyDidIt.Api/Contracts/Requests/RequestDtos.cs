using System.ComponentModel.DataAnnotations;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Entities;

namespace AiAlreadyDidIt.Api.Contracts.Requests;

public class CreateAppRequestRequest
{
    [Required, MinLength(5), MaxLength(160)] public string Title { get; set; } = string.Empty;
    [Required, MinLength(20), MaxLength(4000)] public string Description { get; set; } = string.Empty;
}

public class AppRequestDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RequesterUsername { get; set; }
    public RequestSource Source { get; set; }
    public AppRequestStatus Status { get; set; }
    public AppCardDto? FulfilledBy { get; set; }
    public int VoteCount { get; set; }
    public bool MyVote { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Existing apps that might already satisfy the request (semantic).</summary>
    public List<AppCardDto> Suggestions { get; set; } = [];
}

public class FulfilRequestRequest
{
    public int AppId { get; set; }
}
