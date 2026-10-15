using Microsoft.JSInterop;
using PortalEmpleadoGeotime.Client.Data.DataBaseLocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PortalEmpleadoGeotime.Client.Modelo.Utils
{
    public class ConfigFile
    {           
        public async static Task<bool> ExistsConfigFile()
        {
            MarcasDBLocal laborDBLocal = new();
            return await laborDBLocal.GetParametroApiLocalAsync(1) != null;
        }

        public async static Task<cParametroApiLocal> GetConfigFile() {
            MarcasDBLocal laborDBLocal = new();
            return await laborDBLocal.GetParametroApiLocalAsync(1);
        }

        public static cParametroApiLocal ImportarArchivoConfiguracion(string fileSource)
        {
            cParametroApiLocal parametros = new();

            if (System.IO.File.Exists(fileSource))
            {
                using (XmlReader reader = XmlReader.Create(fileSource))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name.ToString())
                            {
                                case "ApiGeoTimeUrl":
                                    parametros.ApiGeoTimeUrl = reader.ReadString();
                                    break;
                                case "ApiGeotimeDataBase":
                                    parametros.ApiGeotimeDataBase = reader.ReadString();
                                    break;
                                case "ApiGeotimeSchema":
                                    parametros.ApiGeotimeSchema = reader.ReadString();
                                    break;
                                case "ApiClientId":
                                    parametros.ApiClientId = reader.ReadString();
                                    break;
                                case "ApiUser":
                                    parametros.ApiUser = reader.ReadString();
                                    break;
                                case "ApiPassword":
                                    parametros.ApiPassword = reader.ReadString();
                                    break;
                                                               

                            }
                        }
                    }
                }

            }

            return parametros;
        }


    }
}
