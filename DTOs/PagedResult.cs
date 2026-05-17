namespace IslamiJindegiApi.DTOs;

public record PagedResult<T>(IEnumerable<T> Data, int Total, int Page, int PageSize);
