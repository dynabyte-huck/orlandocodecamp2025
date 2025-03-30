namespace AspireDaprDemo.ServiceDefaults.SharedContracts;

public record AppStateEntry<T>(string AppId, T Entity);