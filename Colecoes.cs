namespace ProjetoISP.Baldan
{
    /// <summary>
    /// Nomes das coleções do Firestore. Centralizado aqui para não repetir
    /// "textos soltos" nos controllers (um erro de digitação criaria uma coleção nova).
    /// </summary>
    public static class Colecoes
    {
        public const string Usuarios = "usuarios";
        public const string Categorias = "categorias";
        public const string ModelosChecklist = "modelosChecklist";
        public const string ItensChecklist = "itensChecklist";
        public const string Inspecoes = "inspecoes";
        public const string ResultadosInspecao = "resultadosInspecao";
        public const string PlanosAcao = "planosAcao";
        public const string HistoricosPrazo = "historicosPrazo";
        public const string HistoricosPergunta = "historicosPergunta";
        public const string ModelosSetores = "modelosSetores";
        public const string Topicos = "topicos";
        public const string ChecklistPerguntas = "checklistsPerguntas";
        public const string Setores = "setores";
        public const string Unidades = "unidades";
    }
}
