using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface ITimezoneService
{
    (TimezoneResponse? Result, string? Error) Resolve(double latitude, double longitude);
}
