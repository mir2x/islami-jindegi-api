namespace IslamiJindegiApi.DTOs;

public record MadrasahInfoItem(
    Guid? Id,
    string Label,
    string Info,
    int Position);

public record MadrasahPhotoItem(
    Guid? Id,
    string Title,
    string ImageUrl,
    int Position);

public record MadrasahListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    int Position,
    int InfoCount,
    int PhotoCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record MadrasahDetail(
    Guid Id,
    string Title,
    string? Excerpt,
    string Introduction,
    int Position,
    List<MadrasahInfoItem> Infos,
    List<MadrasahPhotoItem> Photos,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SaveMadrasahRequest(
    string Title,
    string? Excerpt,
    string Introduction,
    int? Position,
    List<MadrasahInfoItem> Infos,
    List<MadrasahPhotoItem> Photos);
