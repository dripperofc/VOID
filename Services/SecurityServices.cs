using System;
using Sodium;
using System.Text;

namespace Void.Services
{
    public class SecurityService
    {
        // MyIdentity guarda sua chave pública e privada.
        public KeyPair MyIdentity { get; private set; }

        public SecurityService()
        {
            // Gera um par de chaves novo toda vez que o app abre (por enquanto)
            MyIdentity = PublicKeyBox.GenerateKeyPair();
        }

        // Transforma a mensagem em "ruído" que só quem tem a chave abre
        public string BlindMessage(string plainText, byte[] recipientPublicKey)
        {
            // Criamos um selo (SealedBox). O servidor não consegue abrir isso.
            byte[] encrypted = SealedPublicKeyBox.Create(plainText, recipientPublicKey);
            return Convert.ToBase64String(encrypted);
        }

        // Abre a mensagem se ela foi enviada para você
        public string UnblindMessage(string cipherText)
        {
            try 
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] decrypted = SealedPublicKeyBox.Open(cipherBytes, MyIdentity.PrivateKey, MyIdentity.PublicKey);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch 
            {
                return "[ERRO: Chave incompatível. O servidor não pode ler isso.]";
            }
        }

        public string GetMyInvite() => Convert.ToBase64String(MyIdentity.PublicKey);
    }
}