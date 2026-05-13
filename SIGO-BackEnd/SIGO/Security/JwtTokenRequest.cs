namespace SIGO.Security
{
    public class JwtTokenRequest
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public int? OficinaId { get; set; }
    }
}
