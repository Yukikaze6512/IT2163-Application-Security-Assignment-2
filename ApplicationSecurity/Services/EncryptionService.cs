using Microsoft.AspNetCore.DataProtection;

namespace ApplicationSecurity.Services
{
    /// <summary>
    /// Provides encryption and decryption for sensitive data (e.g., NRIC)
    /// using ASP.NET Core Data Protection API (AES-256-CBC under the hood).
    /// </summary>
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
