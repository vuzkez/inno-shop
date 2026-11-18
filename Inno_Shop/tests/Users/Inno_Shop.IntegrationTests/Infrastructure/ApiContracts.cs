namespace Inno_Shop.IntegrationTests.Infrastructure;

/// <summary>Response shapes the API produces, mirrored for deserialization in tests.</summary>
public sealed record RegisterResponse(string UserId, string Message);

public sealed record TokensResponse(string AccessToken, string RefreshToken);

public sealed record ProfileResponse(
    string Id,
    string Email,
    string Name,
    string Role,
    bool IsActive,
    bool IsEmailConfirmed);

public sealed record PagedUsersResponse(
    IReadOnlyList<ProfileResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
