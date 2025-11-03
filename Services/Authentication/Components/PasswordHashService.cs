using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

// 1. Servicio de Hash de Contraseñas

namespace MedCenter.Services.Authentication.Components
{
    public class PasswordHashService
    {
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 100000; // Recomendado por OWASP

        /// <summary>
        /// Hashea una contraseña usando PBKDF2 con sal aleatoria
        /// </summary>
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("La contraseña no puede estar vacía");

            // Generar sal aleatoria
            byte[] salt = new byte[SaltSize]; // la sal se agrega para poner bytes aleatorios y por ende que te qeden partes aleatorias en el hash, asi los hashes nunca van a ser iguales aunque las contrasenias en texto plano lo sean
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Generar hash
            byte[] hash = GenerateHash(password, salt);

            // Combinar sal + hash para almacenar
            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            // Convertir a Base64 para almacenar en DB
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verifica si una contraseña coincide con el hash almacenado
        /// </summary>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                // Convertir hash almacenado de Base64
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);

                // Extraer sal (primeros 16 bytes)
                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // Extraer hash (siguientes 32 bytes)
                byte[] hash = new byte[HashSize];
                Array.Copy(hashBytes, SaltSize, hash, 0, HashSize);

                // Generar hash de la contraseña ingresada
                byte[] testHash = GenerateHash(password, salt); //Este metodo devuelve solo los 32 bytes de hash, sin la sal

                // Comparar hashes de forma segura (evita timing attacks)
                return SlowEquals(hash, testHash);
            }
            catch
            {
                return false;
            }
        }

        private byte[] GenerateHash(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(HashSize);
        }

        /// <summary>
        /// Comparación segura de arrays (previene timing attacks)
        /// </summary>
        private bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
    }
}