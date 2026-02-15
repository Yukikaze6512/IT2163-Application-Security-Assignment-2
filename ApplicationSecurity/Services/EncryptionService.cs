using Microsoft.AspNetCore.DataProtection;

namespace ApplicationSecurity.Services
{
    public class EncryptionService
    {
        private readonly IDataProtector _protector;

        public EncryptionService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("AceJobAgency.SensitiveData.v1");
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;
            return _protector.Protect(plainText);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;
            return _protector.Unprotect(cipherText);
        }
    }
}
