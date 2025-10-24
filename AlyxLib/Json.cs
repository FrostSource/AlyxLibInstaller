using System.Text.Json;

namespace AlyxLib;

public static class Json
{
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}