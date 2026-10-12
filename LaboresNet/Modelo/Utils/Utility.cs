
namespace PortalEmpleadoGeotime.Client.Modelo.Utils
{
    public static class Utility
    {
        
        //Función para generar el código de seguridad
        public static string GeneraCodigoSeguridad()
        {
            var characters = "0123456789";

            var Charsarr = new char[8];
            var random = new Random();

            for (int i = 0; i < Charsarr.Length; i++)
            {
                Charsarr[i] = characters[random.Next(characters.Length)];
            }

            var resultString = new String(Charsarr);

            return resultString;
        }

		
	}
}
