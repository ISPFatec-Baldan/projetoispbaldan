using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.PlanosAcao
{
    [FirestoreData]
    public class PlanoAcao5W2H
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string DescricaoDesvio { get; set; }

        [FirestoreProperty]
        public string AcaoCorretiva { get; set; }

        [FirestoreProperty]
        public Timestamp Prazo { get; set; }

        [FirestoreProperty]
        public string Status { get; set; }

        [FirestoreProperty]
        public string IdResultadoInspecao { get; set; }

        [FirestoreProperty]
        public string IdResponsavelUsuario { get; set; }
    }
}