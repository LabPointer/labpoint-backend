namespace Data;

/// <summary>
/// Busca por trecho que funciona nos dois provedores. Use dentro das consultas assim:
/// <code>EF.Functions.Like(x.Name.ToLower(), pattern, TextSearch.Escape)</code>
/// com <c>pattern = TextSearch.ContainsPattern(termo)</c> (já em minúsculas). Substitui o ILIKE, que só existe no
/// PostgreSQL. Observação: no SQLite, <c>lower()</c> só converte letras sem acento.
/// </summary>
public static class TextSearch
{
    public const string Escape = "\\";

    /// <summary>"%termo%" em minúsculas, escapando %, _ e \ digitados pelo usuário.</summary>
    public static string ContainsPattern(string term)
    {
        var escaped = term.Trim().ToLowerInvariant().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return $"%{escaped}%";
    }
}
