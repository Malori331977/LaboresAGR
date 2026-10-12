using System.ComponentModel.DataAnnotations;

namespace PortalEmpleadoGeotime.Client.Modelo
{
    public class DataPassword
    {
        [Required(ErrorMessage = "La contraseña es requerida.")]
        [StringLength(25, ErrorMessage = "Debe tener entre 5 y 25 Caracteres", MinimumLength = 5)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirmar la contraseña es requerido.")]
        [StringLength(25, ErrorMessage = "Debe tener entre 5 y 25 Caracteres", MinimumLength = 5)]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}
