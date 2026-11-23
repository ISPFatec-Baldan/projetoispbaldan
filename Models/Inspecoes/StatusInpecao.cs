namespace ProjetoISP.Baldan.Models.Inspecoes
{
    /// <summary>Status possíveis de uma inspeção (texto único usado em todo o sistema).</summary>
    public static class StatusInspecao
    {
        public const string EmAndamento = "Em andamento";
        public const string Finalizada = "Finalizada";
        public const string Cancelada = "Cancelada";

        public static readonly string[] Todos = { EmAndamento, Finalizada, Cancelada };
    }
}
