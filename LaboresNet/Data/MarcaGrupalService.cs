using com.gsitcr.geotime.Models;
using com.gsitcr.geotime.Models.Request;
using com.gsitcr.geotime.Models.Response;
using com.gsitcr.geotime.Models.Utils;
using PortalEmpleadoGeotime.Client.Data.DataBaseLocal;
using PortalEmpleadoGeotime.Client.Data.Interfaces;
using PortalEmpleadoGeotime.Client.Data.State;

namespace PortalEmpleadoGeotime.Client.Data
{
    internal class MarcaGrupalService : IMarcaGrupalService
    {
        private readonly IGeoTimeConnect _geo;
        private readonly MarcasDBLocal _db = new();
        private readonly LoginState _login;
        private string? _ultimoError;

        public string? UltimoError => _ultimoError;

        public MarcaGrupalService(IGeoTimeConnect geo, LoginState login)
        {
            _geo = geo;
            _login = login;
        }

        public async Task<cMarcaGrupalResultado> Registrar(cEmpleado empleado, int tipo, UbicacionSesion ubicacion, byte[]? foto)
        {
            _ultimoError = null;
            var ahora = HoraLocal();
            var request = new cMarcaGrupalRequest
            {
                idsupervisor = _login.Empleado.IdNumero!,
                idnumero = empleado.IdNumero!,
                tipo = tipo,
                fecha = ahora.Date,
                hora = ahora.ToString("HH:mm"),
                fecha_reg = DateTime.Now,
                lat_reg = ubicacion.Latitud,
                long_reg = ubicacion.Longitud,
                dir_ip = ubicacion.DirIp,
                imagen_reg = foto,
            };

            for (int intento = 1; intento <= 2; intento++)
            {
                try
                {
                    var resultado = await _geo.PostMarcaGrupal(request);
                    if (resultado.EsOk)
                        await GuardarLocal(request, empleado, 'S', null);
                    return resultado;
                }
                catch (Exception ex) when (intento == 1)
                {
                    // un reintento ante falla técnica
                    _ultimoError = $"{ex.GetType().Name}: {ex.Message}";
                }
                catch (Exception ex)
                {
                    _ultimoError = $"{ex.GetType().Name}: {ex.Message}";
                    break;
                }
            }

            return await RegistrarSinConexion(request, empleado);
        }

        private async Task<cMarcaGrupalResultado> RegistrarSinConexion(cMarcaGrupalRequest request, cEmpleado empleado)
        {
            // GetMarcaInDelDiaAsync filtra en memoria (sqlite-net no traduce ToString(formato) sobre fechas)
            var marcasDelDia = (await _db.GetMarcaInDelDiaAsync(request.fecha))
                .Where(m => m.idnumero == request.idnumero && m.estado != 'R');
            var opciones = await _db.GetPhOpcionesAsync() ?? new cPh_Opciones { UTILIZA_DESC = 'T', NUM_ALM = 99 };
            var fechaHora = request.fecha.AddMinutes(ReglasMarcaGrupal.HoraAMinutos(request.hora));

            var resultado = ReglasMarcaGrupal.Evaluar(request.tipo, fechaHora, marcasDelDia, opciones, null);
            if (resultado.EsOk)
                await GuardarLocal(request, empleado, 'N', request.imagen_reg);
            resultado.ModoSinConexion = true;
            return resultado;
        }

        private Task<int> GuardarLocal(cMarcaGrupalRequest request, cEmpleado empleado, char estado, byte[]? foto) =>
            _db.SaveMarcaGrupalAsync(new cMarcaIn
            {
                idtarjeta = empleado.Tarjeta,
                fecha = request.fecha,
                hora = request.hora,
                idterminal = "WPE",
                tipo = request.tipo,
                idplanilla = empleado.IdPlanilla,
                idnumero = request.idnumero,
                estado = estado,
                fecha_reg = request.fecha_reg,
                long_reg = request.long_reg,
                lat_reg = request.lat_reg,
                imagen_reg = foto,
                host = request.dir_ip,
                dir_ip = request.dir_ip,
            }, request.idsupervisor);

        public async Task<(int Enviadas, int Rechazadas)> SincronizarPendientes()
        {
            int enviadas = 0, rechazadas = 0;
            foreach (var p in await _db.GetMarcaGrupalPendienteAsync())
            {
                var request = new cMarcaGrupalRequest
                {
                    idsupervisor = p.IDSUPERVISOR ?? "",
                    idnumero = p.idnumero ?? "",
                    tipo = p.tipo ?? 0,
                    fecha = p.fecha ?? DateTime.Today,
                    hora = p.hora ?? "",
                    fecha_reg = p.fecha_reg ?? DateTime.Now,
                    lat_reg = p.lat_reg,
                    long_reg = p.long_reg,
                    dir_ip = p.dir_ip,
                    imagen_reg = p.imagen_reg,
                    origenOffline = true,
                };

                cMarcaGrupalResultado resultado;
                try
                {
                    resultado = await _geo.PostMarcaGrupal(request);
                }
                catch (Exception)
                {
                    break; // sin conexión: se reintenta en la próxima sincronización
                }

                if (resultado.EsOk)
                {
                    await _db.ActualizarEstadoMarcaGrupalAsync(p.Id!.Value, 'S', null);
                    enviadas++;
                }
                else
                {
                    await _db.ActualizarEstadoMarcaGrupalAsync(p.Id!.Value, 'R', resultado.Mensaje);
                    rechazadas++;
                }
            }
            return (enviadas, rechazadas);
        }

        // Misma zona horaria que usa EmpleadoMarcaList (LoginState.TimeZone = desfase en horas)
        private DateTime HoraLocal()
        {
            var zona = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(z => z.BaseUtcOffset.Hours == _login.TimeZone);
            return zona is null ? DateTime.Now : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
        }

        public DateTime Ahora() => HoraLocal();
    }
}
