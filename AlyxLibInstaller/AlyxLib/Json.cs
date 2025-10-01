using System.Text.Json;

namespace AlyxLibInstaller.AlyxLib;

public static class Json
{
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}