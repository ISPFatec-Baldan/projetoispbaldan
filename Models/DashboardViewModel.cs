namespace ProjetoISP.Baldan.Models
{
    /// <summary>Um setor da grade "Checklists por setor" (Ok = checklist finalizado na semana).</summary>
    public record SetorStatusItem(string Nome, bool Ok);

    /// <summary>Opção do filtro "Por setor" das recorrências.</summary>
    public record SetorOpcao(string Id, string Nome);

    /// <summary>Uma fatia do gráfico "Status de ações".</summary>
    public record StatusAcaoItem(string Status, int Quantidade, string Cor);

    /// <summary>Variação de um indicador (ex.: +3 pts vs. semana anterior).</summary>
    public record Tendencia(decimal Variacao, string Unidade, bool AumentoEhBom, string Referencia);

    /// <summary>Item do painel "Atenção necessária". Tom: "alerta" ou "perigo".</summary>
    public record AlertaItem(string Titulo, string Detalhe, string Tom, string Controlador);

    /// <summary>Dados exibidos na tela inicial (painel).</summary>
    public class DashboardViewModel
    {
        // null = ainda não há respostas na semana (a tela mostra "—")
        public decimal? TaxaConformidadeSemana { get; set; }
        public Tendencia? TendenciaConformidade { get; set; }

        public int PlanosPendentesAnalise { get; set; }
        public Tendencia? TendenciaPlanos { get; set; }

        public int NaoConformidadesReincidentes { get; set; }
        public Tendencia? TendenciaReincidentes { get; set; }

        public int TotalInspecoesMes { get; set; }
        public Tendencia? TendenciaInspecoes { get; set; }

        public int ChecklistsRealizadosSemana { get; set; }
        public List<SetorStatusItem> SetoresSemana { get; set; } = new();
        public List<SetorOpcao> SetoresFiltro { get; set; } = new();

        public string PeriodoComparativo { get; set; } = string.Empty;
        public List<StatusAcaoItem> StatusAcoes { get; set; } = new();

        public List<AlertaItem> Alertas { get; set; } = new();

        /// <summary>True enquanto "planos pendentes" e o gráfico usam dados de exemplo.</summary>
        public bool PlanosEGraficoSaoExemplo { get; set; }

        /// <summary>
        /// Base com DADOS DE EXEMPLO só para o que ainda não lê do Firestore
        /// (planos pendentes de análise e gráfico de status). O PainelService sobrescreve o restante.
        /// </summary>
        public static DashboardViewModel Exemplo() => new()
        {
            PlanosPendentesAnalise = 15,
            TendenciaPlanos = new Tendencia(-2, "", false, "semana anterior"),

            PeriodoComparativo = "Ações ISP 537 ~ 538",
            StatusAcoes = new List<StatusAcaoItem>
            {
                new("Concluído", 52, "#15803D"),
                new("Em andamento", 18, "#0B2A5B"),
                new("Aberto", 30, "#B45309"),
                new("Cancelado", 5, "#6B7280")
            },

            PlanosEGraficoSaoExemplo = true
        };
    }
}
