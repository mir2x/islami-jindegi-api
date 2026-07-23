namespace IslamiJindegiApi.DTOs;

public record GoogleSignInRequest(string IdToken);

public record AuthResponse(string Token, string Email);
