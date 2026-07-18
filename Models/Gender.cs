namespace WaitingForTheSummer.Models;

public enum Gender
{
    Male = 0,
    Female = 1
}

public static class TeamNames
{
    public static string For(Gender gender) => gender switch
    {
        Gender.Male => "Команда парней",
        Gender.Female => "Команда девушек",
        _ => gender.ToString()
    };
}
