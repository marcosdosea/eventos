namespace Core.DTO;

/// <summary>
/// Resultado paginado genérico para listagens.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
