namespace GloboTicket.Frontend.Extensions;

public static class EnumerableOfGuid
{
    public static string ToQueryString(this IEnumerable<Guid> guids)
    {
        return "?" + string.Join("&", guids.Select(g => $"eventIds={g}"));
    }
}
