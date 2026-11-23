using ProjetoISP.Baldan.Models.Checklist;

namespace ProjetoISP.Baldan.Services
{
    /// <summary>
    /// Regra semanal dos checklists: perguntas incluídas ou retiradas passam a valer na PRÓXIMA semana
    /// (segunda-feira, 00:00). Para configurar o sistema pela primeira vez, dá para fazer as mudanças
    /// valerem na hora: appsettings.json -> "Checklist": { "MudancasValem": "imediata" }.
    /// </summary>
    public class VigenciaService
    {
        public bool Imediata { get; }

        public VigenciaService(IConfiguration configuracao)
        {
            Imediata = string.Equals(configuracao["Checklist:MudancasValem"], "imediata", StringComparison.OrdinalIgnoreCase);
        }

        public static DateTime InicioDaSemanaLocal(DateTime local) =>
            local.Date.AddDays(-(((int)local.DayOfWeek + 6) % 7));

        public DateTime ProximaSemanaLocal() => InicioDaSemanaLocal(DateTime.Now).AddDays(7);

        /// <summary>Quando uma mudança feita AGORA passa a valer (em UTC).</summary>
        public DateTime InicioDeVigenciaUtc() =>
            Imediata ? DateTime.UtcNow : DateTime.SpecifyKind(ProximaSemanaLocal(), DateTimeKind.Local).ToUniversalTime();

        public string TextoVigencia() =>
            Imediata ? "imediatamente (modo de configuração)" : $"a partir de {ProximaSemanaLocal():dd/MM/yyyy} (próxima semana)";

        /// <summary>A pergunta está no checklist na data informada?</summary>
        public static bool Vigente(ChecklistPergunta vinculo, DateTime utc) =>
            vinculo.VigenteDesde.ToDateTime() <= utc
            && (vinculo.VigenteAte == null || utc < vinculo.VigenteAte.Value.ToDateTime());
    }
}
