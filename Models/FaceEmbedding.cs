namespace AttendanceAPI.Models
{
    public class FaceEmbedding
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Embedding { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
    }
}
