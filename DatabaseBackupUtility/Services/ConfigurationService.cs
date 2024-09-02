using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
namespace DatabaseBackupUtility.Configs;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationRoot _configuration;
    private readonly string _filePath;

    public ConfigurationService(string filePath)
    {
        _filePath = filePath;

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(filePath, optional: false, reloadOnChange: true);

        _configuration = builder.Build();
    }

    public T GetSection<T>(string sectionName) where T : new()
    {
        var section = _configuration.GetSection(sectionName);
        var config = new T();
        foreach (var property in typeof(T).GetProperties())
        {
            var value = section[property.Name];
            if (value != null)
            {
                property.SetValue(config, Convert.ChangeType(value, property.PropertyType));
            }
        }
        return config;
    }

    public void SetSection<T>(string sectionName, T value)
    {
        var json = File.ReadAllText(_filePath);
        var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        if (jsonObj.ContainsKey(sectionName))
        {
            jsonObj[sectionName] = value;
        }
        else
        {
            jsonObj.Add(sectionName, value);
        }

        File.WriteAllText(_filePath, JsonConvert.SerializeObject(jsonObj, Formatting.Indented));
    }

    public void SaveConfiguration()
    {
        // В этом методе уже сохранено изменение через SetSection, но можно добавить доп. логику
    }
}