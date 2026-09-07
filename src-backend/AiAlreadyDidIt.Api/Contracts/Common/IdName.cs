namespace AiAlreadyDidIt.Api.Contracts.Common;

public record IdNameDto(int Id, string Name);

public record IdSlugNameDto(int Id, string Slug, string Name);

public record CountDto(int Count);

public record OkDto(bool Ok = true);
