namespace WaitingForTheSummer.Models;

/// <summary>Роль игрока в параллельной игре (мафия).</summary>
public enum SideGameRole
{
    Civilian = 1, // Мирный
    Sheriff = 2,  // Шериф
    Mafia = 3,    // Мафия
    Don = 4       // Дон
}

public static class SideGameRoleExtensions
{
    public static string ToDisplayName(this SideGameRole role) => role switch
    {
        SideGameRole.Civilian => "Мирный",
        SideGameRole.Sheriff => "Шериф",
        SideGameRole.Mafia => "Мафия",
        SideGameRole.Don => "Дон",
        _ => role.ToString()
    };

    public static string ToCssClass(this SideGameRole role) => role switch
    {
        SideGameRole.Civilian => "role-civilian",
        SideGameRole.Sheriff => "role-sheriff",
        SideGameRole.Mafia => "role-mafia",
        SideGameRole.Don => "role-don",
        _ => string.Empty
    };
}
