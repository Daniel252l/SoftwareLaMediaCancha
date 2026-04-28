using System;
using System.Text;

namespace LaMediaCancha.Services
{
    /// <summary>
    /// </summary>
    public class SoundexService
    {
        private static readonly char[] CodigoSoundex =
        {

              '0','1','2','3','0','1','2','0','0','2','2','4','5',
              '5','0','1','2','6','2','3','0','1','0','2','0','2'
        };

        public string CalcularSoundex(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "0000";

            texto = texto.ToUpper();
            var sb = new StringBuilder();
            sb.Append(texto[0]);  

            char codigoAnterior = GetCodigo(texto[0]);

            for (int i = 1; i < texto.Length && sb.Length < 4; i++)
            {
                char c = texto[i];
                if (!char.IsLetter(c)) continue;

                char codigo = GetCodigo(c);
                if (codigo != '0' && codigo != codigoAnterior)
                {
                    sb.Append(codigo);
                    codigoAnterior = codigo;
                }
            }

            while (sb.Length < 4) sb.Append('0');

            return sb.ToString();
        }

        private static char GetCodigo(char letra)
        {
            int idx = letra - 'A';
            if (idx < 0 || idx >= CodigoSoundex.Length) return '0';
            return CodigoSoundex[idx];
        }
    }
}