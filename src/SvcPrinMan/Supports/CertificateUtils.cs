using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SvcPrinMan.Supports
{
    public class CertificateUtils
    {
        public static X509Certificate2 CreateSelfSignedCertificateAsync(
            string CA = "MoHA Corp .Inc",
            int validForDays = 30)
        {
            using RSA rootRSA = RSA.Create(4096);
            using RSA certRSA = RSA.Create(2048);
            var certAuthorityReq = new CertificateRequest(
                $"CN={CA}",
                rootRSA,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            certAuthorityReq.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));
            certAuthorityReq.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(certAuthorityReq.PublicKey, false));
            using var rootCertificate = certAuthorityReq
                .CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-30),
                DateTimeOffset.UtcNow.AddDays(365));

            var csr = new CertificateRequest(
                $"CN={CA}",
                certRSA,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            csr.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));
            csr.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature
                    | X509KeyUsageFlags.NonRepudiation
                    | X509KeyUsageFlags.CrlSign
                    | X509KeyUsageFlags.DataEncipherment
                    | X509KeyUsageFlags.KeyAgreement
                    | X509KeyUsageFlags.KeyCertSign
                    | X509KeyUsageFlags.KeyEncipherment,
                    false));
            csr.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.8") }, true));
            csr.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(csr.PublicKey, false));
            using var certificate = csr.Create(
                rootCertificate,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(validForDays),
                new byte[] { 1, 2, 3, 4 });
            return certificate.CopyWithPrivateKey(certRSA);
        }

        public static string GetProofJWTFromExistingCertificate(
            X509Certificate2 signingCert, Guid appOrSpObjectId)
        {
            // audience - this has to be hardcoded according to Microsoft 
            // https://docs.microsoft.com/en-us/graph/application-rollkey-prooftoken
            var aud = $"00000002-0000-0000-c000-000000000000";
            var claims = new Dictionary<string, object>()
            {
                { "aud", aud },
                { "iss", appOrSpObjectId }
            };
            // token validity should not be more than 10 minutes
            var now = DateTime.UtcNow;
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                NotBefore = now,
                Expires = now.AddMinutes(10),
                SigningCredentials = new X509SigningCredentials(signingCert)
            };
            return new JsonWebTokenHandler().CreateToken(securityTokenDescriptor);
        }

        public static string GeneratePEMWithPrivateKeyAsString(X509Certificate2 certificate)
        {
            var sb = new StringBuilder();
            AsymmetricAlgorithm key = certificate.GetRSAPrivateKey();
            byte[] privKeyBytes = key.ExportPkcs8PrivateKey();
            char[] privKeyPem = PemEncoding.Write("PRIVATE KEY", privKeyBytes);
            char[] certificatePem = PemEncoding.Write("CERTIFICATE", certificate.GetRawCertData());
            sb.AppendLine(new string(privKeyPem));
            sb.AppendLine();
            sb.AppendLine(new string(certificatePem));
            return sb.ToString();
        }

        public static byte[] GetPfxAsBytes(X509Certificate2 certificate)
        {
            return certificate.Export(X509ContentType.Pfx);
        }
    }
}
