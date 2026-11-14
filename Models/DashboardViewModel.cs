namespace ProjetoISP.Baldan.Models
{
    /// <summary>Um setor da lista "Checklist realizados por setor da atual semana".</summary>
    public record SetorStatusItem(string Nome, bool Ok);

    /// <summary>Uma fatia do gráfico de pizza "Status de Ações ISP".</summary>
    public record StatusAcaoItem(string Status, int Quantidade, string Cor);

    /// <summary>Dados exibidos na tela inicial (dashboard).</summary>
    public class DashboardViewModel
    {
        public decimal TaxaConformidadeSemana { get; set; }
        public int PlanosPendentesAnalise { get; set; }
        public int NaoConformidadesReincidentes { get; set; }
        public int TotalInspecoesMes { get; set; }

        public int ChecklistsRealizadosSemana { get; set; }
        public List<SetorStatusItem> SetoresSemana { get; set; } = new();

        public string PeriodoComparativo { get; set; } = string.Empty;
        public List<StatusAcaoItem> StatusAcoes { get; set; } = new();

        /// <summary>
        /// DADOS DE EXEMPLO (iguais aos do protótipo do Figma).
        /// Serão trocados por consultas ao Firestore quando as regras de cada indicador estiverem definidas.
        /// </summary>
        public static DashboardViewModel Exemplo() => new()
        {
            TaxaConformidadeSemana = 92,
            PlanosPendentesAnalise = 15,
            NaoConformidadesReincidentes = 5,
            TotalInspecoesMes = 120,

            ChecklistsRealizadosSemana = 12,
            SetoresSemana = new List<SetorStatusItem>
            {
                new("Almoxarifado / CD", true),
                new("Expedição", false),
                new("Ferramentaria", true),
                new("Fábrica Disco", true),
                new("Fundição", true),
                new("Manutenção", true),
                new("Mercado", false),
                new("Pátio de Recebimento", true),
                new("Pintura / Montagem", true),
                new("Peça Avulsa", true),
                new("Pré-Fabricação", true),
                new("Protótipo", false),
                new("Solda", true),
                new("Usinagem", true),
                new("Taquaritinga", true),
                new("Unidade 2", true)
            },

            PeriodoComparativo = "Ações ISP 537 ~ 538",
            StatusAcoes = new List<StatusAcaoItem>
            {
                new("Concluído", 52, "#6E9C8B"),
                new("Aberto", 30, "#FF0000"),
                new("Em andamento", 18, "#002147"),
                new("Cancelado", 5, "#9B9494")
            }
        };
    }
}
