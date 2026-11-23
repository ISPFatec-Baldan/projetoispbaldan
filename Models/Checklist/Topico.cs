using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Checklist
{
    /// <summary>Tópico que organiza as perguntas (ex.: Estrutura, Sinalização).</summary>
    [FirestoreData]
    public class Topico
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string Nome { get; set; }
    }
}
