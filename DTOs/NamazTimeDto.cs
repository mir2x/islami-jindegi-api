namespace IslamiJindegiApi.DTOs;

public record NamazTimeListItem(
    Guid Id,
    string Title,
    string? TitleBn,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record NamazTimeDetail(
    Guid Id,
    string Title,
    string? TitleBn,
    string Masail,
    string? Fazail,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SaveNamazTimeRequest(
    string Title,
    string? TitleBn,
    string Masail,
    string? Fazail,
    int? Position);
