using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValosModeler
{
	internal class EncryptTool
	{
        public static string Encrypt(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return s;
            }
            else
            {
                var encoding = new UTF8Encoding();
                byte[] plain = encoding.GetBytes(s);
                byte[] secret = System.Security.Cryptography.ProtectedData.Protect(plain, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(secret);
            }
        }
        public static string Decrypt(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return s;
            }
            else
            {
                byte[] secret = Convert.FromBase64String(s);
                byte[] plain = System.Security.Cryptography.ProtectedData.Unprotect(secret, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                var encoding = new UTF8Encoding();
                return encoding.GetString(plain);
            }
        }
    }
}
