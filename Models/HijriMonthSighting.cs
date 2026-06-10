namespace IslamiJindegiApi.Models;

public class HijriMonthSighting
{
    public Guid Id { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public int HijriYear { get; set; }
    public int HijriMonth { get; set; }
    public DateOnly GregorianStartDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
