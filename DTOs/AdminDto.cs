namespace IslamiJindegiApi.DTOs;

public record AdminResponse(Guid Id, string Email, string? DisplayName, DateTime CreatedAt);

public record CreateAdminRequest(string Email, string? DisplayName);
