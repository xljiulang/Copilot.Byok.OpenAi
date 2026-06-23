using Copilot.Byok.OpenAi.Uitls;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Copilot.Byok.OpenAi.Services
{
    /// <summary>
    /// 证书服务，负责生成和管理CA证书
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class CertService
    {
        private readonly IMemoryCache serverCertCache;
        private readonly ILogger<CertService> logger;
        private X509Certificate2? caCert;

        /// <summary>
        /// 获取CA证书文件路径
        /// </summary>
        public string CaCerFilePath { get; }

        /// <summary>
        /// 获取CA私钥文件路径
        /// </summary>
        public string CaKeyFilePath { get; }

        /// <summary>
        /// 初始化证书服务实例
        /// </summary>
        /// <param name="serverCertCache">服务器证书缓存</param>
        /// <param name="logger">日志记录器</param>
        public CertService(
            IMemoryCache serverCertCache,
            ILogger<CertService> logger)
        {
            this.serverCertCache = serverCertCache;
            this.logger = logger;

            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(Copilot.Byok.OpenAi));
            Directory.CreateDirectory(appDataPath);

            this.CaCerFilePath = Path.Combine(appDataPath, "ca.crt");
            this.CaKeyFilePath = Path.Combine(appDataPath, "ca.key");
        }

        /// <summary>
        /// 生成CA证书（如果不存在）
        /// </summary>
        /// <returns>如果生成了新证书则返回true，否则返回false</returns>
        public bool CreateCaCertIfNotExists()
        {
            if (File.Exists(this.CaCerFilePath) && File.Exists(this.CaKeyFilePath))
            {
                return false;
            }

            File.Delete(this.CaCerFilePath);
            File.Delete(this.CaKeyFilePath);

            var notBefore = DateTimeOffset.Now.AddDays(-1);
            var notAfter = DateTimeOffset.Now.AddYears(10);

            var subjectName = new X500DistinguishedName($"CN={nameof(Copilot.Byok.OpenAi)}");
            this.caCert = CertGenerator.CreateCACertificate(subjectName, notBefore, notAfter);

            var privateKeyPem = this.caCert.GetRSAPrivateKey()?.ExportRSAPrivateKeyPem();
            File.WriteAllText(this.CaKeyFilePath, new string(privateKeyPem), Encoding.UTF8);

            var certPem = this.caCert.ExportCertificatePem();
            File.WriteAllText(this.CaCerFilePath, new string(certPem), Encoding.UTF8);

            return true;
        }

        /// <summary>
        /// 安装和信任CA证书（仅在Windows上）
        /// </summary>
        public void InstallCaCertIfWindows()
        {
            this.logger.LogInformation($"CA证书路径：{this.CaCerFilePath}");

            if (OperatingSystem.IsWindows() == false)
            {
                return;
            }

            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);

                var pem = File.ReadAllText(this.CaCerFilePath);
                var caCert = X509Certificate2.CreateFromPem(pem);
                var subjectName = caCert.Subject[3..];
                foreach (var item in store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false))
                {

                    if (item.Thumbprint != caCert.Thumbprint)
                    {
                        store.Remove(item);
                    }
                }
                if (store.Certificates.Find(X509FindType.FindByThumbprint, caCert.Thumbprint, true).Count == 0)
                {
                    store.Add(caCert);
                }
                store.Close();
            }
            catch (Exception)
            {
                logger.LogWarning($"请手动安装CA证书{this.CaCerFilePath}到\"将所有的证书都放入下列存储\"受信任的根证书颁发机构\"");
            }
        }

        /// <summary>
        /// 获取或创建颁发给指定域名的证书
        /// </summary>
        /// <param name="domain">域名</param>
        /// <returns>服务器证书</returns>
        public X509Certificate2 GetOrCreateServerCert(string? domain)
        {
            if (this.caCert == null)
            {
                this.caCert = X509Certificate2.CreateFromPemFile(this.CaCerFilePath, this.CaKeyFilePath);
            }

            var key = $"{nameof(CertService)}:{domain}";
            var endCert = this.serverCertCache.GetOrCreate(key, GetOrCreateCert);
            return endCert!;

            // 生成域名的1年证书
            X509Certificate2 GetOrCreateCert(ICacheEntry entry)
            {
                var notBefore = DateTimeOffset.Now.AddDays(-1);
                var notAfter = DateTimeOffset.Now.AddYears(1);
                entry.SetAbsoluteExpiration(notAfter);

                var subjectName = new X500DistinguishedName($"CN={domain}");
                var endCert = CertGenerator.CreateEndCertificate(this.caCert, subjectName, null, notBefore, notAfter);

                // 重新初始化证书，以兼容win平台不能使用内存证书
                return X509CertificateLoader.LoadPkcs12(endCert.Export(X509ContentType.Pfx), null);
            }
        }
    }
}
