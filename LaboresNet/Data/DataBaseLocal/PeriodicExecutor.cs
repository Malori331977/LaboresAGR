using com.gsitcr.geotime.Models;
using PortalEmpleadoGeotime.Client.Data.Interfaces;
using PortalEmpleadoGeotime.Client.Data.State;
using System.Timers;

namespace PortalEmpleadoGeotime.Client.Data.DataBaseLocal
{
	public class JobExecutedEventArgs : EventArgs { }


	public class PeriodicExecutor : IDisposable
	{
		public event EventHandler<JobExecutedEventArgs> JobExecuted;
        private readonly IGeoTimeConnect _geoConnect;
        private readonly LoginState _loginState;
        private readonly IMarcaGrupalService _marcaGrupal;
        public PeriodicExecutor(IGeoTimeConnect geoConnect, LoginState loginState, IMarcaGrupalService marcaGrupal)
        {
            _geoConnect = geoConnect;
            _loginState = loginState;
            _marcaGrupal = marcaGrupal;
        }
        void OnJobExecuted()
		{
			JobExecuted?.Invoke(this, new JobExecutedEventArgs());
		}

		System.Timers.Timer _Timer;
		bool _Running;

		public void StartExecuting()
		{
			if (!_Running)
			{
				// Initiate a Timer
				_Timer = new System.Timers.Timer();
				_Timer.Interval = 600000;  // every 5 mins
				_Timer.Elapsed += HandleTimer;
				_Timer.AutoReset = true;
				_Timer.Enabled = true;

				_Running = true;
			}
		}
		async void HandleTimer(object source, ElapsedEventArgs e)
		{
            // Execute required job
            if (await _geoConnect.ApiDisponible())
                await SincronizarBases();

            // Notify any subscribers to the event
            OnJobExecuted();
		}

        private async Task SincronizarBases()
        {
            try
            {
                MarcasDBLocal marcaDBLocal = new();

                //sincronizacion de DB Local a DB Sql
                var marcasLocal = await marcaDBLocal.GetMarcaPendiente();
                List<cMarcaIn> listMarcaIn = new();
                if (marcasLocal.Count() > 0)
                {
                    foreach (var marcaLocal in marcasLocal)
                    {
                        cMarcaIn marcaIn = new cMarcaIn
                        {
                            idtarjeta = marcaLocal.idtarjeta,
                            fecha = marcaLocal.fecha,
                            hora = marcaLocal.hora,
                            idterminal = marcaLocal.idterminal,
                            tipo = marcaLocal.tipo,
                            idplanilla = marcaLocal.idplanilla,
                            idnumero = marcaLocal.idnumero,
                            estado = 'N',
                            fecha_reg = marcaLocal.fecha_reg,
                            long_reg = marcaLocal.long_reg,
                            lat_reg = marcaLocal.lat_reg,
                            imagen_reg = marcaLocal.imagen_reg,
                            gps_cell = marcaLocal.gps_cell,
                            host = marcaLocal.host,
                            dir_ip = marcaLocal.dir_ip,
                        };
                        listMarcaIn.Add(marcaIn);
                    }
                   
                    var respuesta = await _geoConnect.Post("MarcaIn", listMarcaIn);
                    
                    if (respuesta.Id == "0")
                    {
                        foreach (var marca in marcasLocal)
                        {
                            marca.estado = 'S';
                            var resp = await marcaDBLocal.SaveMarcaInAsync(marca);

                        }
                    }

                }

                // Marcas grupal automatica pendientes (se envian a /MarcaGrupal con sus reglas)
                await _marcaGrupal.SincronizarPendientes();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Dispose()
		{
			if (_Running)
			{
				// Clear up the timer
			}
		}
	}
}
