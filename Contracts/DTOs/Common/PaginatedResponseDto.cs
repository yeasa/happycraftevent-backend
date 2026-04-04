namespace HappyCraftEvent.Contracts.DTOs.Common;

public class PaginatedResponseDto<T>
{
    public int              PageNumber { get; set; }
    public int              PagePerRow { get; set; }
    public int              TotalCount { get; set; }
    public IEnumerable<T>   Data       { get; set; } = [];
}
