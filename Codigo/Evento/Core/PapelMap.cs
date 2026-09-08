namespace Core;

/// <summary>
/// Fonte única de verdade para mapeamento idPapel -&gt; role do Identity.
/// 1=ADMINISTRADOR, 2=GESTOR, 3=COLABORADOR, 4=USUARIO, 5=PARTICIPANTE.
/// </summary>
public static class PapelMap
{
    public static readonly IReadOnlyList<string> AllRoles =
        new[] { "ADMINISTRADOR", "GESTOR", "COLABORADOR", "USUARIO", "PARTICIPANTE" };

    public static string ToRole(int idPapel) => idPapel switch
    {
        1 => "ADMINISTRADOR",
        2 => "GESTOR",
        3 => "COLABORADOR",
        4 => "USUARIO",
        5 => "PARTICIPANTE",
        _ => throw new ArgumentException($"Papel inválido: {idPapel}.", nameof(idPapel)),
    };
}
