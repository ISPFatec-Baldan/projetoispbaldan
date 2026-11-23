namespace ProjetoISP.Baldan.Models.Checklist
{
    /// <summary>Uma linha da lista de perguntas da tela "Editar Checklist".</summary>
    public record PerguntaLinha(ItemChecklist Item, int TotalRespostas, bool AvisoNaoAplicavel);

    /// <summary>Dados da tela "Editar Checklist".</summary>
    public class EditarChecklistViewModel
    {
        public ModeloChecklist Modelo { get; set; } = new();
        public string NomeCategoria { get; set; } = "—";
        public List<string> NomesSetores { get; set; } = new();
        public List<PerguntaLinha> Perguntas { get; set; } = new();

        /// <summary>recentes | antigas | alfabetica | aviso | desativadas</summary>
        public string Filtro { get; set; } = "recentes";

        public int TotalAtivas { get; set; }
        public int TotalDesativadas { get; set; }

        /// <summary>Quantos checklists seguidos "não aplicável" geram o aviso laranja.</summary>
        public int UltimosChecklists { get; set; }
    }
}
