using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;

namespace PortalEmpleadoGeotime.Client.Modelo.Utils
{
    public class Utiles
    {
        public static async Task SetFocus(InputText txt)
        {
            if (txt == null) return;

            if (txt.Element.HasValue)
            {
                await txt.Element.Value.FocusAsync();
            }
        }

        public static async Task SetFocus(InputNumber<decimal> txt)
        {
            if (txt == null) return;

            if (txt.Element.HasValue)
            {
                await txt.Element.Value.FocusAsync();
            }
        }

        public static string GetTipoMarca(int tipo)
        {
            string strTipo="Indefinido";
            switch (tipo)
            {
                case 1: strTipo = "Entrada"; break;
                case 2: strTipo = "Salida"; break;
                case 3: strTipo = "Descanso"; break;

            }
            return strTipo;
        }

        /// <summary>
        /// Get substring of specified number of characters on the right.
        /// </summary>
        public static string Right(string value, int length)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            return value.Length <= length ? value : value.Substring(value.Length - length);
        }

        public static string Global_encrypt(string texto)
        {
            SHA1 sha1 = SHA1.Create();
            byte[] textOriginal = Encoding.Default.GetBytes(texto);
            byte[] hash = sha1.ComputeHash(textOriginal);
            StringBuilder cadena = new StringBuilder();
            foreach (byte i in hash)
            {
                cadena.AppendFormat("{0:x2}", i);
            }
            return cadena.ToString();
        }

        public static IEnumerable<int> PageSizeOptions()
        {
            return new int[] { 7, 10, 25, 50, 100, 500 };
        }
    }
}
