namespace ProjetoISP.Baldan.Models.Inspecoes
{
    /// <summary>Valores padronizados de ResultadoInspecao.RespostaColetada.</summary>
    public static class RespostasPadrao
    {
        public const string Conforme = "Conforme";
        public const string NaoConforme = "Não conforme";
        public const string NaoAplicavel = "Não aplicável";

        public static readonly string[] Todas = { Conforme, NaoConforme, NaoAplicavel };
    }
}
