using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Setores
{
    [FirestoreData]
    public class SetorArea
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string Nome { get; set; }

        [FirestoreProperty]
        public string UrlFotoEquipe { get; set; }

        [FirestoreProperty]
        public Timestamp DataCriacao { get; set; } // Firestore utiliza Timestamp para datas

        [FirestoreProperty]
        public string IdUnidade { get; set; } // Referência para o ID da Unidade
    }
}


