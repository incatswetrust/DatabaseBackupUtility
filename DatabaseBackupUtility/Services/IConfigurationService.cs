namespace DatabaseBackupUtility.Configs;

public interface IConfigurationService
{
    T GetSection<T>(string sectionName) where T : new();
    void SetSection<T>(string sectionName, T value);
    void SaveConfiguration();
}