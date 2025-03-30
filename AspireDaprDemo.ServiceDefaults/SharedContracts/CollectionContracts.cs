namespace AspireDaprDemo.ServiceDefaults.SharedContracts;

public class PagedCollection<T>(IEnumerable<T> items)
{
    public string? PageIterationToken { get; set; }

    public int PageSize { get; set; }

    public IEnumerable<T> Items { get; set; } = items;
}