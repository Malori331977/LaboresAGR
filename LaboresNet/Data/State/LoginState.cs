
using com.gsitcr.geotime.Models;
using PortalEmpleadoGeotime.Client.Modelo;


namespace PortalEmpleadoGeotime.Client.Data.State
{
    public class LoginState
    {
        public cEmpleado Empleado { get; set; }
        public cPh_Login? PhLogin { get; set; }
        public cPh_Usuario? PhUsuario { get; set; }

        public event Action OnChange;

        public IEnumerable<NavItem> ItemsMenu = Enumerable.Empty<NavItem>();
        public bool InMarcador { get; set; } = false;
        public int TimeZone { get; set; } = 1;

        public string CompaniaSeleccionada { get; set; } = "";
        public string PaisSeleccionado { get; set; } = "";

        public LoginState()
        {
            Empleado = null;
        }


        public void SetLogin(cEmpleado empleado)
        {
            Empleado = empleado;
            NotifyStateChanged();
        }

        public void SetLogin(cPh_Login phLogin, cPh_Usuario phUsuario)
        {
            PhLogin = phLogin;
            PhUsuario = phUsuario;
            NotifyStateChanged();
        }

        public void SetCompania(string compania)
        {
            CompaniaSeleccionada = compania;
            NotifyStateChanged();
        }

        public void SetPais(string pais)
        {
            PaisSeleccionado = pais;
            NotifyStateChanged();
        }

        public void SetTimeZone(int timeZone)
        {
            TimeZone = timeZone;
            NotifyStateChanged();
        }

        public void SetItemsMenu(IEnumerable<NavItem> _itemsMenu)
        {
            ItemsMenu = _itemsMenu;
            NotifyStateChanged();
        }

        public void SetMarcador(bool inMarcador)
        {
            InMarcador = inMarcador;
            NotifyStateChanged();
        }
        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}
