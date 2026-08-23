namespace IslamiJindegiApi.DTOs;

public record AppSettingsResponse(bool AskQuestion, bool DisplayOfflineQuran);

public record UpdateAppSettingsRequest(bool AskQuestion, bool DisplayOfflineQuran);
