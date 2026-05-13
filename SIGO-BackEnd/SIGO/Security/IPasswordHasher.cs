namespace SIGO.Security
{
    public interface IPasswordHasher
    {
        string Hash(string input);
        bool Verify(string input, string hashedValue);
        bool NeedsRehash(string hashedValue);
    }
}
