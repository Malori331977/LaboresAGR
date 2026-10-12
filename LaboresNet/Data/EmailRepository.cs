using com.gsitcr.geotime.Models;
using com.gsitcr.geotime.Models.Response;
using Microsoft.Extensions.Configuration;
using PortalEmpleadoGeotime.Client.Data.Interfaces;
using PortalEmpleadoGeotime.Client.Modelo;
using Email = com.gsitcr.geotime.Models.Email;


namespace PortalEmpleadoGeotime.Client.Data
{
    public class EmailRepository : IEmailRepository
    {

        private readonly IGeoTimeConnect _geoConnect;
        public EmailRepository(IConfiguration configuration, IGeoTimeConnect geoConnect)
        {
            _geoConnect = geoConnect;
        }

  
        public async Task<EventResponse> EnviaCorreoCambioPassword(cEmpleado empleado, string codigo)
        {
            
            string cuerpoHtml = "<!DOCTYPE html>" +
                "<html><head>Hola, " + empleado.Nombre + "</head>" +
                "<body><br><br>Se solicitó recientemente cambiar la contraseña de su cuenta en el portal de Marcas Web. Si usted solicitó este cambio de contraseña, utilice el código siguiente para establecer una nueva contraseña." +
                "<br>El código de seguridad que debe indicar en la aplicación es el siguiente: " +
                "<br><h3>" + codigo.Trim() + "</h3><br><br>" +
                "<br><br>Si no desea cambiar su contraseña, ignore este mensaje.</body><footer><br><hr>" +
                "Este mensaje se generó de forma automática.  Por favor no contestar.</footer></html>";

            Email email = new();
            email.Asunto = "Confirmación del restablecimiento de la contraseña en el Portal de Marcas Web";
            email.Cuerpo = cuerpoHtml;
            email.Para = empleado.Email!;
            email.Adjunto = "";
            email.CC = "";
            email.StreamAdjunto = new byte[0];
            email.De = "default";
            email.SmtpServer = "default";

            EventResponse respuesta = new();
            List<Email> emails = new List<Email>() { email };
            respuesta = await _geoConnect.Post("EnviarNotificacion", emails);

            return respuesta;
        }

    }
    
}
