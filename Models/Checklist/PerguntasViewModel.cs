namespace ProjetoISP.Baldan.Models.Checklist
{
    public record ChecklistOpcao(string Id, string Nome);

    /// <summary>Situação: vigente, saindo (vai sair na virada da semana) ou entrando (ainda não começou).</summary>
    public record VinculoPergunta(string IdChecklist, string NomeChecklist, string Situacao, DateTime? Desde, DateTime? Ate);

    public record PerguntaItem(
        ItemChecklist Item, string NomeTopico, List<VinculoPergunta> Vinculos,
        int TotalRespostas, bool AvisoNaoAplicavel, bool EmUso);

    /// <summary>Dados da tela "Perguntas".</summary>
    public class PerguntasViewModel
    {
        public List<PerguntaItem> Perguntas { get; set; } = new();
        public List<Topico> Topicos { get; set; } = new();
        public List<ChecklistOpcao> Checklists { get; set; } = new();

        public string Ordem { get; set; } = "recentes";
        public string Situacao { get; set; } = "ativas";
        public string? IdChecklist { get; set; }
        public string? IdTopico { get; set; }

        public int TotalAtivas { get; set; }
        public int TotalDesativadas { get; set; }
        public int UltimosChecklists { get; set; }

        /// <summary>Texto que diz quando as mudanças nos checklists passam a valer.</summary>
        public string TextoVigencia { get; set; } = string.Empty;
    }
}
