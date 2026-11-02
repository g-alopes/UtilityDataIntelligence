namespace UtilityData.Api.Imports;
public sealed class ImportStorageOptions
{
    public const string SectionName = "ImportStorage";
    public string RootPath { get; set; } = "App_Data/imports";
    public long MaxFileBytes { get; set; } = 50 * 1024 * 1024;
}
