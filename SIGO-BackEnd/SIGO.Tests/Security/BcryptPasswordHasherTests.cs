using SIGO.Security;
using Xunit;

namespace SIGO.Tests.Security
{
    public class BcryptPasswordHasherTests
    {
        [Fact]
        public void Hash_DeveGerarHashBcryptVerificavel()
        {
            var hasher = new BcryptPasswordHasher();

            var hash = hasher.Hash("senha-forte-123");

            Assert.StartsWith("$2", hash);
            Assert.True(hasher.Verify("senha-forte-123", hash));
            Assert.False(hasher.Verify("senha-errada", hash));
            Assert.False(hasher.NeedsRehash(hash));
        }

        [Fact]
        public void Verify_DeveAceitarHashSha256LegadoParaMigração()
        {
            var hasher = new BcryptPasswordHasher();
            const string legacySha256For123 = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";

            Assert.True(hasher.Verify("123", legacySha256For123));
            Assert.True(hasher.NeedsRehash(legacySha256For123));
        }
    }
}
