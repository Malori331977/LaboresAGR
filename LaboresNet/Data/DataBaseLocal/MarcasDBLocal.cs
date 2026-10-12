
using com.gsitcr.geotime.Models;
using PortalEmpleadoGeotime.Client.Modelo;
using SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using TicoFrutLibs.Models;


namespace PortalEmpleadoGeotime.Client.Data.DataBaseLocal
{
    public class MarcasDBLocal
    {
        public static MarcasDBLocal instance;
        readonly SQLiteAsyncConnection database;

        public MarcasDBLocal()
        {
            try
            {
                string dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AvancesMoDB");
                Directory.CreateDirectory(dbFolder);
                string dbPath = Path.Combine(dbFolder, "LaboresDatabase.db3");

                database = new SQLiteAsyncConnection(dbPath);
                // Use local table definitions that store single-char values as string
                database.CreateTableAsync<cEmpleadoLocal>().Wait();
                database.CreateTableAsync<cMarcaInLocal>().Wait();
                database.CreateTableAsync<ParametroSincLocal>().Wait();
                database.CreateTableAsync<cPh_PeriodosLocal>().Wait();
                database.CreateTableAsync<cPh_PlanillaLocal>().Wait();
                database.CreateTableAsync<cPh_CompaniaLocal>().Wait();
                database.CreateTableAsync<cPortal_ConfigLocal>().Wait();
                database.CreateTableAsync<NavItemLocal>().Wait();
                database.CreateTableAsync<cPh_UsuarioLocal>().Wait();
                database.CreateTableAsync<cParametroApiLocal>().Wait();
                database.CreateTableAsync<cPhLoginLocal>().Wait();

                //database.CreateTableAsync<cPh_ProyectoLocal>().Wait();
                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS cPh_Proyecto (
                    PROYECTO TEXT NOT NULL,
                    DESCRIPCION TEXT NOT NULL,
                    CENTRO_COSTO TEXT,
                    PRIMARY KEY (PROYECTO, CENTRO_COSTO)
                );").Wait();

                // Create cPh_FaseProyecto with a composite primary key (PROYECTO, FASE)
                // SQLite-net doesn't directly support composite keys via attributes, so create the table manually if it doesn't exist.
                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS cPh_FaseProyecto (
                    PROYECTO TEXT NOT NULL,
                    FASE TEXT NOT NULL,
                    NOMBRE TEXT,
                    DESCRIPCION TEXT NOT NULL,
                    ACEPTA_DATOS TEXT NOT NULL,
                    PRIMARY KEY (PROYECTO, FASE)
                );").Wait();
                database.CreateTableAsync<cMarcaDistribucionConceptoLocal>().Wait();
                database.CreateTableAsync<cCentroCostoLocal>().Wait();
                //database.ExecuteAsync(@"DROP TABLE IF EXISTS cTemplateFACE").Wait();
                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS cTemplateFACE (
                    IDNUMERO TEXT NOT NULL,
                    INDEXID INTEGER NOT NULL,
                    FACETEMPLATE BLOB,
                    PRIMARY KEY (IDNUMERO, INDEXID)
                );").Wait();

                //database.CreateTableAsync<cPh_ProyectoLocal>().Wait();
                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS cEstado (
                    ID INT NOT NULL,
                    DESCRIPCION TEXT NOT NULL,
                    PRIMARY KEY (ID)
                );").Wait();

                // ── TicoFrut tables ──────────────────────────────────────────────
                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrFinca (
                    Compania TEXT NOT NULL,
                    Pais     TEXT NOT NULL,
                    Bloque   TEXT NOT NULL,
                    Finca    TEXT NOT NULL,
                    PRIMARY KEY (Compania, Pais, Bloque, Finca)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrLote (
                    Compania  TEXT NOT NULL,
                    Bloque    TEXT NOT NULL,
                    Finca     TEXT NOT NULL,
                    Lote      TEXT NOT NULL,
                    Pais      TEXT,
                    TipoSuelo TEXT,
                    Variedad  TEXT,
                    PRIMARY KEY (Compania, Bloque, Finca, Lote)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrOt (
                    Compania TEXT NOT NULL,
                    Bloque   TEXT NOT NULL,
                    Finca    TEXT NOT NULL,
                    Ot       TEXT NOT NULL,
                    NombreOt TEXT,
                    PRIMARY KEY (Compania, Bloque, Finca, Ot)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrOtLabor (
                    Compania    TEXT NOT NULL,
                    Bloque      TEXT NOT NULL,
                    Finca       TEXT NOT NULL,
                    Ot          TEXT NOT NULL,
                    Labor       TEXT NOT NULL,
                    NombreLabor TEXT,
                    PRIMARY KEY (Compania, Bloque, Finca, Ot, Labor)
                );").Wait();
                //database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrRecursoConcepto").Wait();
                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrRecursoConcepto (
                    Compania     TEXT NOT NULL,
                    Recurso      TEXT NOT NULL,
                    Descripcion  TEXT,
                    TipoFactorMo TEXT,
                    FactorMo     REAL,
                    ConceptoMo   TEXT,
                    Unidad   TEXT,
                    PRIMARY KEY (Compania, Recurso)
                );").Wait();

                // Migración: recrear tablas con IdRegistro como parte de la PK compuesta.
                // Se eliminan las tablas previas para aplicar el nuevo esquema.
                //database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDet").Wait();
                //database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliar").Wait();

                // Migración (un solo uso, ya aplicada): GeoAgrLaborAuxiliarDet ahora incluye
                // IdEmpleadoAsignado en su PK y GeoAgrLaborAuxiliarDetEmpAsig se elimina por completo.
                // IMPORTANTE: estos DROP deben permanecer comentados. El constructor se ejecuta en cada
                // 'new MarcasDBLocal()', por lo que dejarlos activos borraba la tabla de detalles en cada
                // instanciación (incluido el propio GuardarDatos al sincronizar), perdiendo los registros.
                //database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDet").Wait();
                //database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDetEmpAsig").Wait();
                database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDetEmpAsig").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrLaborAuxiliar (
                    IdRegistro  INTEGER NOT NULL,
                    Compania    TEXT    NOT NULL,
                    IdEmpleado  TEXT    NOT NULL,
                    Fecha       TEXT    NOT NULL,
                    Comentarios TEXT,
                    EstadoId    INTEGER NOT NULL,
                    PRIMARY KEY (IdRegistro, Compania, IdEmpleado, Fecha)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrLaborAuxiliarDet (
                    IdRegistro          INTEGER NOT NULL,
                    Compania            TEXT    NOT NULL,
                    IdEmpleado          TEXT    NOT NULL,
                    Fecha               TEXT    NOT NULL,
                    Item                INTEGER NOT NULL,
                    Bloque              TEXT,
                    Finca               TEXT,
                    Lote                TEXT,
                    Ot                  TEXT,
                    Labor               TEXT,
                    Recurso             TEXT,
                    CantidadRendimiento REAL,
                    Unidad              TEXT,
                    HorasAprobadas      REAL,
                    EstadoId            INTEGER NOT NULL,
                    IdEmpleadoAsignado  TEXT    NOT NULL,
                    PRIMARY KEY (IdRegistro, Compania, IdEmpleado, Fecha, Item, IdEmpleadoAsignado),
                    FOREIGN KEY (IdRegistro, Compania, IdEmpleado, Fecha)
                        REFERENCES GeoAgrLaborAuxiliar (IdRegistro, Compania, IdEmpleado, Fecha)
                        ON DELETE CASCADE
                );").Wait();

                database.CreateTableAsync<FiltroUsuarioLocal>().Wait();

            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            
        }

        private cEmpleado MapEmpleadoLocalToEmpleado(cEmpleadoLocal l)
        {
            // Map properties manually - include all present local properties
            return new cEmpleado
            {
                IdNumero = l.IdNumero,
                IdPlanilla = l.IdPlanilla,
                Nombre = l.Nombre,
                Tarjeta = l.Tarjeta,
                Identificacion = l.Identificacion,
                IdDepartamento = l.IdDepartamento,
                Estado = string.IsNullOrEmpty(l.Estado) ? '\0' : l.Estado[0],
                IdCCosto = l.IdCCosto,
                Fecha_Ingreso = l.Fecha_Ingreso,
                Email = l.Email,
                IdGrupo = l.IdGrupo,
                IdHorario = l.IdHorario,
                IdAgrupamiento = l.IdAgrupamiento,
                Tipo_Marca = l.Tipo_Marca,
                global_clave = l.global_clave,
                Fecha_Salida = l.Fecha_Salida,
                global_code = l.global_code,
                fecha_act_code = l.fecha_act_code,
                foto = l.foto,
                exporta = string.IsNullOrEmpty(l.exporta) ? 'F' : l.exporta[0],
                ubicacion = l.ubicacion,
                rubro1 = l.rubro1,
                rubro2 = l.rubro2,
                rubro3 = l.rubro3,
                rubro4 = l.rubro4,
                rubro5 = l.rubro5,
                rubro6 = l.rubro6,
                rubro7 = l.rubro7,
                rubro8 = l.rubro8,
                rubro9 = l.rubro9,
                rubro10 = l.rubro10,
                rubro11 = l.rubro11,
                rubro12 = l.rubro12,
                rubro13 = l.rubro13,
                rubro14 = l.rubro14,
                rubro15 = l.rubro15,
                rubro16 = l.rubro16,
                rubro17 = l.rubro17,
                rubro18 = l.rubro18,
                rubro19 = l.rubro19,
                rubro20 = l.rubro20,
                rubro21 = l.rubro21,
                rubro22 = l.rubro22,
                rubro23 = l.rubro23,
                rubro24 = l.rubro24,
                rubro25 = l.rubro25,
                inicio_rol = l.inicio_rol,
                web_pass = l.web_pass,
                id_transfo_conc = l.id_transfo_conc,
                widioma = l.widioma,
                def_cc = l.def_cc,
                def_py = l.def_py,
                def_fase = l.def_fase,
                puesto = l.puesto,
                TCompensacionAprobado = l.TCompensacionAprobado,
                TCompensacionUtilizado = l.TCompensacionUtilizado,
                Sexo = l.Sexo,
                FechaNacimiento = l.FechaNacimiento
            };
        }


        public async Task<List<cEmpleado>> GetEmpleadoAsync()
        {
            var locals = await database.Table<cEmpleadoLocal>().ToListAsync();
            return locals.Select(l => MapEmpleadoLocalToEmpleado(l)).ToList();
        }

        public async Task<cEmpleado> GetEmpleadoAsync(string id)
        {
            var l = await database.Table<cEmpleadoLocal>()
                            .FirstOrDefaultAsync(i => i.IdNumero == id);
            if (l is null) return null;
            return MapEmpleadoLocalToEmpleado(l);
        }

        public async Task<cEmpleado> GetEmpleadoByEmailAsync(string email)
        {
            var l = await database.Table<cEmpleadoLocal>()
                            .FirstOrDefaultAsync(i => i.Email == email);
            if (l is null) return null;
            return MapEmpleadoLocalToEmpleado(l);
        }

        public async Task<int> SaveEmpleadoAsync(cEmpleado empleado)
        {
            try
            {
                var local = await database.Table<cEmpleadoLocal>().FirstOrDefaultAsync(i => i.IdNumero == empleado.IdNumero);

                if (local is not null)
                {
                    local.Estado = empleado.Estado==null? "F" : empleado.Estado.ToString();
                    local.Nombre = empleado.Nombre;
                    local.Tarjeta = empleado.Tarjeta;
                    local.Identificacion = empleado.Identificacion;
                    local.IdCCosto = empleado.IdCCosto;
                    local.IdPlanilla = empleado.IdPlanilla;
                    local.IdDepartamento = empleado.IdDepartamento;
                    local.puesto = empleado.puesto;
                    local.global_clave = empleado.global_clave;
                    local.global_code = empleado.global_code;
                    local.Fecha_Ingreso = empleado.Fecha_Ingreso;
                    local.Email = empleado.Email;
                    local.IdGrupo = empleado.IdGrupo;
                    local.IdHorario = empleado.IdHorario;
                    local.IdAgrupamiento = empleado.IdAgrupamiento;
                    local.Tipo_Marca = empleado.Tipo_Marca;
                    local.Fecha_Salida = empleado.Fecha_Salida;
                    local.fecha_act_code = empleado.fecha_act_code;
                    local.foto = empleado.foto;
                    local.exporta = empleado.exporta == null ? "F" : empleado.exporta.ToString();
                    local.ubicacion = empleado.ubicacion;
                    local.rubro1 = empleado.rubro1;
                    local.rubro2 = empleado.rubro2;
                    local.rubro3 = empleado.rubro3;
                    local.rubro4 = empleado.rubro4;
                    local.rubro5 = empleado.rubro5;
                    local.rubro6 = empleado.rubro6;
                    local.rubro7 = empleado.rubro7;
                    local.rubro8 = empleado.rubro8;
                    local.rubro9 = empleado.rubro9;
                    local.rubro10 = empleado.rubro10;
                    local.rubro11 = empleado.rubro11;
                    local.rubro12 = empleado.rubro12;
                    local.rubro13 = empleado.rubro13;
                    local.rubro14 = empleado.rubro14;
                    local.rubro15 = empleado.rubro15;
                    local.rubro16 = empleado.rubro16;
                    local.rubro17 = empleado.rubro17;
                    local.rubro18 = empleado.rubro18;
                    local.rubro19 = empleado.rubro19;
                    local.rubro20 = empleado.rubro20;
                    local.rubro21 = empleado.rubro21;
                    local.rubro22 = empleado.rubro22;
                    local.rubro23 = empleado.rubro23;
                    local.rubro24 = empleado.rubro24;
                    local.rubro25 = empleado.rubro25;
                    local.inicio_rol = empleado.inicio_rol;
                    local.web_pass = empleado.web_pass;
                    local.id_transfo_conc = empleado.id_transfo_conc;
                    local.widioma = empleado.widioma;
                    local.def_cc = empleado.def_cc;
                    local.def_py = empleado.def_py;
                    local.def_fase = empleado.def_fase;
                    local.TCompensacionAprobado = empleado.TCompensacionAprobado;
                    local.TCompensacionUtilizado = empleado.TCompensacionUtilizado;
                    local.Sexo = empleado.Sexo;
                    local.FechaNacimiento = empleado.FechaNacimiento;

                    return await database.UpdateAsync(local);
                }
                else
                {
                    var toInsert = new cEmpleadoLocal
                    {
                        IdNumero = empleado.IdNumero,
                        Estado = empleado.Estado == '\0' ? "" : empleado.Estado.ToString(),
                        Nombre = empleado.Nombre,
                        Tarjeta = empleado.Tarjeta,
                        Identificacion = empleado.Identificacion,
                        IdCCosto = empleado.IdCCosto,
                        IdPlanilla = empleado.IdPlanilla,
                        IdDepartamento = empleado.IdDepartamento,
                        puesto = empleado.puesto,
                        global_clave = empleado.global_clave,
                        global_code = empleado.global_code,
                        Fecha_Ingreso = empleado.Fecha_Ingreso,
                        Email = empleado.Email,
                        IdGrupo = empleado.IdGrupo,
                        IdHorario = empleado.IdHorario,
                        IdAgrupamiento = empleado.IdAgrupamiento,
                        Tipo_Marca = empleado.Tipo_Marca,
                        Fecha_Salida = empleado.Fecha_Salida,
                        fecha_act_code = empleado.fecha_act_code,
                        foto = empleado.foto,
                        exporta = empleado.exporta == null ? "F" : empleado.exporta.ToString(),
                        ubicacion = empleado.ubicacion,
                        rubro1 = empleado.rubro1,
                        rubro2 = empleado.rubro2,
                        rubro3 = empleado.rubro3,
                        rubro4 = empleado.rubro4,
                        rubro5 = empleado.rubro5,
                        rubro6 = empleado.rubro6,
                        rubro7 = empleado.rubro7,
                        rubro8 = empleado.rubro8,
                        rubro9 = empleado.rubro9,
                        rubro10 = empleado.rubro10,
                        rubro11 = empleado.rubro11,
                        rubro12 = empleado.rubro12,
                        rubro13 = empleado.rubro13,
                        rubro14 = empleado.rubro14,
                        rubro15 = empleado.rubro15,
                        rubro16 = empleado.rubro16,
                        rubro17 = empleado.rubro17,
                        rubro18 = empleado.rubro18,
                        rubro19 = empleado.rubro19,
                        rubro20 = empleado.rubro20,
                        rubro21 = empleado.rubro21,
                        rubro22 = empleado.rubro22,
                        rubro23 = empleado.rubro23,
                        rubro24 = empleado.rubro24,
                        rubro25 = empleado.rubro25,
                        inicio_rol = empleado.inicio_rol,
                        web_pass = empleado.web_pass,
                        id_transfo_conc = empleado.id_transfo_conc,
                        widioma = empleado.widioma,
                        def_cc = empleado.def_cc,
                        def_py = empleado.def_py,
                        def_fase = empleado.def_fase,
                        TCompensacionAprobado = empleado.TCompensacionAprobado,
                        TCompensacionUtilizado = empleado.TCompensacionUtilizado,
                        Sexo = empleado.Sexo,
                        FechaNacimiento = empleado.FechaNacimiento
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch(Exception)
            {
                throw;
            }
            
        }

        public async Task<int> SaveEmpleadoAsync(IEnumerable<cEmpleado> empleados)
        {
            var empleadosList = empleados.ToList();
            if (empleadosList.Count == 0) return 0;

            var existingLocals = await database.Table<cEmpleadoLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => l.IdNumero);

            var toInsert = new List<cEmpleadoLocal>();
            var toUpdate = new List<cEmpleadoLocal>();

            foreach (var empleado in empleadosList)
            {
                if (existingDict.TryGetValue(empleado.IdNumero, out var local))
                {
                    local.Estado = empleado.Estado == null ? "F" : empleado.Estado.ToString();
                    local.Nombre = empleado.Nombre;
                    local.Tarjeta = empleado.Tarjeta;
                    local.Identificacion = empleado.Identificacion;
                    local.IdCCosto = empleado.IdCCosto;
                    local.IdPlanilla = empleado.IdPlanilla;
                    local.IdDepartamento = empleado.IdDepartamento;
                    local.puesto = empleado.puesto;
                    local.global_clave = empleado.global_clave;
                    local.global_code = empleado.global_code;
                    local.Fecha_Ingreso = empleado.Fecha_Ingreso;
                    local.Email = empleado.Email;
                    local.IdGrupo = empleado.IdGrupo;
                    local.IdHorario = empleado.IdHorario;
                    local.IdAgrupamiento = empleado.IdAgrupamiento;
                    local.Tipo_Marca = empleado.Tipo_Marca;
                    local.Fecha_Salida = empleado.Fecha_Salida;
                    local.fecha_act_code = empleado.fecha_act_code;
                    local.foto = empleado.foto;
                    local.exporta = empleado.exporta == null ? "F" : empleado.exporta.ToString();
                    local.ubicacion = empleado.ubicacion;
                    local.rubro1 = empleado.rubro1;
                    local.rubro2 = empleado.rubro2;
                    local.rubro3 = empleado.rubro3;
                    local.rubro4 = empleado.rubro4;
                    local.rubro5 = empleado.rubro5;
                    local.rubro6 = empleado.rubro6;
                    local.rubro7 = empleado.rubro7;
                    local.rubro8 = empleado.rubro8;
                    local.rubro9 = empleado.rubro9;
                    local.rubro10 = empleado.rubro10;
                    local.rubro11 = empleado.rubro11;
                    local.rubro12 = empleado.rubro12;
                    local.rubro13 = empleado.rubro13;
                    local.rubro14 = empleado.rubro14;
                    local.rubro15 = empleado.rubro15;
                    local.rubro16 = empleado.rubro16;
                    local.rubro17 = empleado.rubro17;
                    local.rubro18 = empleado.rubro18;
                    local.rubro19 = empleado.rubro19;
                    local.rubro20 = empleado.rubro20;
                    local.rubro21 = empleado.rubro21;
                    local.rubro22 = empleado.rubro22;
                    local.rubro23 = empleado.rubro23;
                    local.rubro24 = empleado.rubro24;
                    local.rubro25 = empleado.rubro25;
                    local.inicio_rol = empleado.inicio_rol;
                    local.web_pass = empleado.web_pass;
                    local.id_transfo_conc = empleado.id_transfo_conc;
                    local.widioma = empleado.widioma;
                    local.def_cc = empleado.def_cc;
                    local.def_py = empleado.def_py;
                    local.def_fase = empleado.def_fase;
                    local.TCompensacionAprobado = empleado.TCompensacionAprobado;
                    local.TCompensacionUtilizado = empleado.TCompensacionUtilizado;
                    local.Sexo = empleado.Sexo;
                    local.FechaNacimiento = empleado.FechaNacimiento;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cEmpleadoLocal
                    {
                        IdNumero = empleado.IdNumero,
                        Estado = empleado.Estado == '\0' ? "" : empleado.Estado.ToString(),
                        Nombre = empleado.Nombre,
                        Tarjeta = empleado.Tarjeta,
                        Identificacion = empleado.Identificacion,
                        IdCCosto = empleado.IdCCosto,
                        IdPlanilla = empleado.IdPlanilla,
                        IdDepartamento = empleado.IdDepartamento,
                        puesto = empleado.puesto,
                        global_clave = empleado.global_clave,
                        global_code = empleado.global_code,
                        Fecha_Ingreso = empleado.Fecha_Ingreso,
                        Email = empleado.Email,
                        IdGrupo = empleado.IdGrupo,
                        IdHorario = empleado.IdHorario,
                        IdAgrupamiento = empleado.IdAgrupamiento,
                        Tipo_Marca = empleado.Tipo_Marca,
                        Fecha_Salida = empleado.Fecha_Salida,
                        fecha_act_code = empleado.fecha_act_code,
                        foto = empleado.foto,
                        exporta = empleado.exporta == null ? "F" : empleado.exporta.ToString(),
                        ubicacion = empleado.ubicacion,
                        rubro1 = empleado.rubro1,
                        rubro2 = empleado.rubro2,
                        rubro3 = empleado.rubro3,
                        rubro4 = empleado.rubro4,
                        rubro5 = empleado.rubro5,
                        rubro6 = empleado.rubro6,
                        rubro7 = empleado.rubro7,
                        rubro8 = empleado.rubro8,
                        rubro9 = empleado.rubro9,
                        rubro10 = empleado.rubro10,
                        rubro11 = empleado.rubro11,
                        rubro12 = empleado.rubro12,
                        rubro13 = empleado.rubro13,
                        rubro14 = empleado.rubro14,
                        rubro15 = empleado.rubro15,
                        rubro16 = empleado.rubro16,
                        rubro17 = empleado.rubro17,
                        rubro18 = empleado.rubro18,
                        rubro19 = empleado.rubro19,
                        rubro20 = empleado.rubro20,
                        rubro21 = empleado.rubro21,
                        rubro22 = empleado.rubro22,
                        rubro23 = empleado.rubro23,
                        rubro24 = empleado.rubro24,
                        rubro25 = empleado.rubro25,
                        inicio_rol = empleado.inicio_rol,
                        web_pass = empleado.web_pass,
                        id_transfo_conc = empleado.id_transfo_conc,
                        widioma = empleado.widioma,
                        def_cc = empleado.def_cc,
                        def_py = empleado.def_py,
                        def_fase = empleado.def_fase,
                        TCompensacionAprobado = empleado.TCompensacionAprobado,
                        TCompensacionUtilizado = empleado.TCompensacionUtilizado,
                        Sexo = empleado.Sexo,
                        FechaNacimiento = empleado.FechaNacimiento
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public async Task<List<cPh_Periodos>> GetPhPeriodoAsync()
        {
            var locals = await database.Table<cPh_PeriodosLocal>().ToListAsync();
            return locals.Select(l => new cPh_Periodos
            {
                idperiodo = l.idperiodo,
                inicio = l.inicio,
                fin = l.fin,
                estado = string.IsNullOrEmpty(l.estado) ? 'N' : l.estado[0],
                tipo_planilla = l.tipo_planilla![0],
                dia_inicio = l.dia_inicio,
                fin_proy = l.fin_proy,
                inicio_proy = l.inicio_proy,
                periodo_proy = l.periodo_proy
            }).ToList();
        }

        public async Task<cPh_Periodos> GetPhPeriodoAsync(string id)
        {
            var l = await database.Table<cPh_PeriodosLocal>()
                            .FirstOrDefaultAsync(i => i.idperiodo == id);
            if (l is null) return null;
            return new cPh_Periodos
            {
                idperiodo = l.idperiodo,
                inicio = l.inicio,
                fin = l.fin,
                estado = string.IsNullOrEmpty(l.estado) ? 'N' : l.estado[0],
                tipo_planilla = l.tipo_planilla![0],
                dia_inicio = l.dia_inicio,
                fin_proy = l.fin_proy,
                inicio_proy = l.inicio_proy,
                periodo_proy = l.periodo_proy
            };
        }

        public async Task<int> SavePhPeriodoAsync(cPh_Periodos periodo)
        {
            try
            {
                var local = await database.Table<cPh_PeriodosLocal>().FirstOrDefaultAsync(i => i.idperiodo == periodo.idperiodo);

                if (local is not null)
                {
                    local.inicio = periodo.inicio;
                    local.fin = periodo.fin;
                    local.estado = periodo.estado == null ? "N" : periodo.estado.ToString();
                    local.tipo_planilla = periodo.tipo_planilla.ToString();
                    local.dia_inicio = periodo.dia_inicio;
                    local.fin_proy = periodo.fin_proy;
                    local.inicio_proy = periodo.inicio_proy;
                    local.periodo_proy = periodo.periodo_proy;

                    return await database.UpdateAsync(local);
                }
                else
                {
                    var toInsert = new cPh_PeriodosLocal
                    {
                        idperiodo = periodo.idperiodo,
                        inicio = periodo.inicio,
                        fin = periodo.fin,
                        estado = periodo.estado == null ? "F" : periodo.estado.ToString(),
                        tipo_planilla = periodo.tipo_planilla.ToString(),
                        dia_inicio = periodo.dia_inicio,
                        fin_proy = periodo.fin_proy,
                        inicio_proy = periodo.inicio_proy,
                        periodo_proy = periodo.periodo_proy
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<int> SavePhPeriodoAsync(IEnumerable<cPh_Periodos> periodos)
        {
            var periodosList = periodos.ToList();
            if (periodosList.Count == 0) return 0;

            var existingLocals = await database.Table<cPh_PeriodosLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => l.idperiodo);

            var toInsert = new List<cPh_PeriodosLocal>();
            var toUpdate = new List<cPh_PeriodosLocal>();

            foreach (var periodo in periodosList)
            {
                if (existingDict.TryGetValue(periodo.idperiodo, out var local))
                {
                    local.inicio = periodo.inicio;
                    local.fin = periodo.fin;
                    local.estado = periodo.estado == null ? "N" : periodo.estado.ToString();
                    local.tipo_planilla = periodo.tipo_planilla.ToString();
                    local.dia_inicio = periodo.dia_inicio;
                    local.fin_proy = periodo.fin_proy;
                    local.inicio_proy = periodo.inicio_proy;
                    local.periodo_proy = periodo.periodo_proy;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cPh_PeriodosLocal
                    {
                        idperiodo = periodo.idperiodo,
                        inicio = periodo.inicio,
                        fin = periodo.fin,
                        estado = periodo.estado == null ? "F" : periodo.estado.ToString(),
                        tipo_planilla = periodo.tipo_planilla.ToString(),
                        dia_inicio = periodo.dia_inicio,
                        fin_proy = periodo.fin_proy,
                        inicio_proy = periodo.inicio_proy,
                        periodo_proy = periodo.periodo_proy
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public async Task<List<cPh_Planilla>> GetPhPlanillaAsync()
        {
            var locals = await database.Table<cPh_PlanillaLocal>().ToListAsync();
            return locals.Select(l => new cPh_Planilla
            {
                idplanilla = l.idplanilla,
                planilla = l.planilla,
                tipo_planilla = l.tipo_planilla![0],
                nom_conector = l.nom_conector,
                nivel_aprob_ext = l.nivel_aprob_ext,
                tipo_adic = l.tipo_adic![0],
                agrup_salida = l.agrup_salida![0],
                ext_det = l.ext_det![0],
                ext_per_ant = l.ext_per_ant![0],
                est_nomina = l.est_nomina,
                tipo_dist = l.tipo_dist![0],
                auto_proceso = l.auto_proceso![0],
                dia_inicio = l.dia_inicio,
                proyecta = l.proyecta![0],
                m_desc = l.m_desc![0],
                c_adic = l.c_adic![0],
                c_inci = l.c_inci![0],
                c_ext = l.c_ext![0],

            }).ToList();
        }

        public async Task<cPh_Planilla> GetPhPlanillaAsync(string id)
        {
            var l = await database.Table<cPh_PlanillaLocal>()
                            .FirstOrDefaultAsync(i => i.idplanilla == id);
            if (l is null) return null;
            return new cPh_Planilla
            {
                idplanilla = l.idplanilla,
                planilla = l.planilla,
                tipo_planilla = l.tipo_planilla![0],
                nom_conector = l.nom_conector,
                nivel_aprob_ext = l.nivel_aprob_ext,
                tipo_adic = l.tipo_adic![0],
                agrup_salida = l.agrup_salida![0],
                ext_det = l.ext_det![0],
                ext_per_ant = l.ext_per_ant![0],
                est_nomina = l.est_nomina,
                tipo_dist = l.tipo_dist![0],
                auto_proceso = l.auto_proceso![0],
                dia_inicio = l.dia_inicio,
                proyecta = l.proyecta![0],
                m_desc = l.m_desc![0],
                c_adic = l.c_adic![0],
                c_inci = l.c_inci![0],
                c_ext = l.c_ext![0],
            };
        }

        public async Task<int> SavePhPlanillaAsync(cPh_Planilla planilla)
        {
            try
            {
                var local = await database.Table<cPh_PlanillaLocal>().FirstOrDefaultAsync(i => i.idplanilla == planilla.idplanilla);

                if (local is not null)
                {
                    local.planilla = planilla.planilla;
                    local.tipo_planilla = planilla.tipo_planilla == null ? null : planilla.tipo_planilla?.ToString();
                    local.nom_conector = planilla.nom_conector;
                    local.nivel_aprob_ext = planilla.nivel_aprob_ext;
                    local.tipo_adic = planilla.tipo_adic?.ToString();
                    local.agrup_salida = planilla.agrup_salida == null ? null : planilla.agrup_salida?.ToString();
                    local.ext_det = planilla.ext_det == null ? null : planilla.ext_det.ToString();
                    local.ext_per_ant = planilla.ext_per_ant == null ? null : planilla.ext_per_ant.ToString();
                    local.est_nomina = planilla.est_nomina;
                    local.tipo_dist = planilla.tipo_dist == null ? null : planilla.tipo_dist?.ToString();
                    local.auto_proceso = planilla.auto_proceso == null ? null : planilla.auto_proceso?.ToString();
                    local.dia_inicio = planilla.dia_inicio;
                    local.proyecta = planilla.proyecta.ToString();
                    local.m_desc = planilla.m_desc == null ? null : planilla.m_desc.ToString();
                    local.c_adic = planilla.c_adic == null ? null : planilla.c_adic?.ToString();
                    local.c_inci = planilla.c_inci == null ? null : planilla.c_inci?.ToString();
                    local.c_ext = planilla.c_ext == null ? null : planilla.c_ext?.ToString();

                    return await database.UpdateAsync(local);
                }
                else
                {
                    var toInsert = new cPh_PlanillaLocal
                    {
                        idplanilla = planilla.idplanilla,
                        planilla = planilla.planilla,
                        tipo_planilla = planilla.tipo_planilla==null?null:planilla.tipo_planilla?.ToString(),
                        nom_conector = planilla.nom_conector,
                        nivel_aprob_ext = planilla.nivel_aprob_ext,
                        tipo_adic = planilla.tipo_adic?.ToString(),
                        agrup_salida = planilla.agrup_salida == null ? null : planilla.agrup_salida?.ToString(),
                        ext_det = planilla.ext_det == null ? null : planilla.ext_det.ToString(),
                        ext_per_ant = planilla.ext_per_ant == null ? null : planilla.ext_per_ant.ToString(),
                        est_nomina = planilla.est_nomina,
                        tipo_dist = planilla.tipo_dist == null ? null : planilla.tipo_dist?.ToString(),
                        auto_proceso = planilla.auto_proceso == null ? null : planilla.auto_proceso?.ToString(),
                        dia_inicio = planilla.dia_inicio,
                        proyecta = planilla.proyecta.ToString(),
                        m_desc = planilla.m_desc == null ? null : planilla.m_desc.ToString(),
                        c_adic = planilla.c_adic == null ? null : planilla.c_adic?.ToString(),
                        c_inci = planilla.c_inci == null ? null : planilla.c_inci?.ToString(),
                        c_ext = planilla.c_ext == null ? null : planilla.c_ext?.ToString()
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<int> SavePhPlanillaAsync(IEnumerable<cPh_Planilla> planillas)
        {
            var planillasList = planillas.ToList();
            if (planillasList.Count == 0) return 0;

            var existingLocals = await database.Table<cPh_PlanillaLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => l.idplanilla);

            var toInsert = new List<cPh_PlanillaLocal>();
            var toUpdate = new List<cPh_PlanillaLocal>();

            foreach (var planilla in planillasList)
            {
                if (existingDict.TryGetValue(planilla.idplanilla, out var local))
                {
                    local.planilla = planilla.planilla;
                    local.tipo_planilla = planilla.tipo_planilla == null ? null : planilla.tipo_planilla?.ToString();
                    local.nom_conector = planilla.nom_conector;
                    local.nivel_aprob_ext = planilla.nivel_aprob_ext;
                    local.tipo_adic = planilla.tipo_adic?.ToString();
                    local.agrup_salida = planilla.agrup_salida == null ? null : planilla.agrup_salida?.ToString();
                    local.ext_det = planilla.ext_det == null ? null : planilla.ext_det.ToString();
                    local.ext_per_ant = planilla.ext_per_ant == null ? null : planilla.ext_per_ant.ToString();
                    local.est_nomina = planilla.est_nomina;
                    local.tipo_dist = planilla.tipo_dist == null ? null : planilla.tipo_dist?.ToString();
                    local.auto_proceso = planilla.auto_proceso == null ? null : planilla.auto_proceso?.ToString();
                    local.dia_inicio = planilla.dia_inicio;
                    local.proyecta = planilla.proyecta.ToString();
                    local.m_desc = planilla.m_desc == null ? null : planilla.m_desc.ToString();
                    local.c_adic = planilla.c_adic == null ? null : planilla.c_adic?.ToString();
                    local.c_inci = planilla.c_inci == null ? null : planilla.c_inci?.ToString();
                    local.c_ext = planilla.c_ext == null ? null : planilla.c_ext?.ToString();
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cPh_PlanillaLocal
                    {
                        idplanilla = planilla.idplanilla,
                        planilla = planilla.planilla,
                        tipo_planilla = planilla.tipo_planilla == null ? null : planilla.tipo_planilla?.ToString(),
                        nom_conector = planilla.nom_conector,
                        nivel_aprob_ext = planilla.nivel_aprob_ext,
                        tipo_adic = planilla.tipo_adic?.ToString(),
                        agrup_salida = planilla.agrup_salida == null ? null : planilla.agrup_salida?.ToString(),
                        ext_det = planilla.ext_det == null ? null : planilla.ext_det.ToString(),
                        ext_per_ant = planilla.ext_per_ant == null ? null : planilla.ext_per_ant.ToString(),
                        est_nomina = planilla.est_nomina,
                        tipo_dist = planilla.tipo_dist == null ? null : planilla.tipo_dist?.ToString(),
                        auto_proceso = planilla.auto_proceso == null ? null : planilla.auto_proceso?.ToString(),
                        dia_inicio = planilla.dia_inicio,
                        proyecta = planilla.proyecta.ToString(),
                        m_desc = planilla.m_desc == null ? null : planilla.m_desc.ToString(),
                        c_adic = planilla.c_adic == null ? null : planilla.c_adic?.ToString(),
                        c_inci = planilla.c_inci == null ? null : planilla.c_inci?.ToString(),
                        c_ext = planilla.c_ext == null ? null : planilla.c_ext?.ToString()
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public async Task<List<cMarcaIn>> GetMarcaIn()
        {
            var locals = await database.Table<cMarcaInLocal>().ToListAsync();
            return locals.Select(l => new cMarcaIn
            {
                fecha = l.fecha,
                hora = l.hora,
                tipo = l.tipo,
                estado = string.IsNullOrEmpty(l.estado) ? 'N' : l.estado[0],
                
            }).ToList();
        }

        public async Task<cMarcaIn> GetMarcaIn(DateTime fecha, string hora, int tipo)
        {
            var l = await database.Table<cMarcaInLocal>().FirstOrDefaultAsync(i => i.fecha == fecha && i.hora == hora && i.tipo == tipo);
            if (l is null) return null;
            return new cMarcaIn
            {
                idtarjeta = string.IsNullOrEmpty(l.idtarjeta) ? null : l.idtarjeta,
                fecha = l.fecha,
                hora = string.IsNullOrEmpty(l.hora) ? null : l.hora,
                idterminal = string.IsNullOrEmpty(l.idterminal) ? null : l.idterminal,
                tipo = l.tipo,
                idplanilla = string.IsNullOrEmpty(l.idplanilla) ? null : l.idplanilla,
                idnumero = string.IsNullOrEmpty(l.idnumero) ? null : l.idnumero,
                estado = string.IsNullOrEmpty(l.estado) ? null : l.estado[0],
                fecha_reg = l.fecha_reg,
                long_reg = string.IsNullOrEmpty(l.long_reg) ? null : l.long_reg,
                lat_reg = string.IsNullOrEmpty(l.lat_reg) ? null : l.lat_reg,
                imagen_reg = l.imagen_reg,
                gps_cell = string.IsNullOrEmpty(l.gps_cell) ? null : l.gps_cell[0],
                host = string.IsNullOrEmpty(l.host) ? null : l.host,
                dir_ip = string.IsNullOrEmpty(l.dir_ip) ? null : l.dir_ip
            };
        }

        public async Task<IEnumerable<cMarcaIn>> GetMarcaIn(DateTime fecha)
        {
            var l = await database.Table<cMarcaInLocal>().Where(i => ((DateTime)i.fecha!).ToString("yyyyMMdd") == fecha.ToString("yyyyMMdd")).ToListAsync();
            if (l is null) return null;
            return l.Select(l => new cMarcaIn
            {
                idtarjeta = string.IsNullOrEmpty(l.idtarjeta) ? null : l.idtarjeta,
                fecha = l.fecha,
                hora = string.IsNullOrEmpty(l.hora) ? null : l.hora,
                idterminal = string.IsNullOrEmpty(l.idterminal) ? null : l.idterminal,
                tipo = l.tipo,
                idplanilla = string.IsNullOrEmpty(l.idplanilla) ? null : l.idplanilla,
                idnumero = string.IsNullOrEmpty(l.idnumero) ? null : l.idnumero,
                estado = string.IsNullOrEmpty(l.estado) ? null : l.estado[0],
                fecha_reg = l.fecha_reg,
                long_reg = string.IsNullOrEmpty(l.long_reg) ? null : l.long_reg,
                lat_reg = string.IsNullOrEmpty(l.lat_reg) ? null : l.lat_reg,
                imagen_reg = l.imagen_reg,
                gps_cell = string.IsNullOrEmpty(l.gps_cell) ? null : l.gps_cell[0],
                host = string.IsNullOrEmpty(l.host) ? null : l.host,
                dir_ip = string.IsNullOrEmpty(l.dir_ip) ? null : l.dir_ip
            }).ToList();
        }

        /// <summary>
        /// Devuelve el conjunto de IdNumero que tienen al menos una marca de tipo 1 (entrada)
        /// para la fecha indicada.  Comparación por fecha sin hora (yyyyMMdd).
        /// </summary>
        public async Task<HashSet<string>> GetIdNumerosConMarcaEntradaAsync(DateTime fecha)
        {
            var fechaStr = fecha.ToString("yyyyMMdd");
            var locals   = await database.Table<cMarcaInLocal>()
                .Where(m => m.tipo == 1)
                .ToListAsync();

            return locals
                .Where(m => !string.IsNullOrEmpty(m.idnumero)
                         && m.fecha.HasValue
                         && m.fecha.Value.ToString("yyyyMMdd") == fechaStr)
                .Select(m => m.idnumero!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<List<cMarcaIn>> GetMarcaPendiente()
        {
            var locals = await database.Table<cMarcaInLocal>().Where(i => i.estado == "N").ToListAsync();
            return locals.Select(l => new cMarcaIn
            {
                idtarjeta = string.IsNullOrEmpty(l.idtarjeta) ? null : l.idtarjeta,
                fecha = l.fecha,
                hora = string.IsNullOrEmpty(l.hora) ? null : l.hora,
                idterminal = string.IsNullOrEmpty(l.idterminal) ? null : l.idterminal,
                tipo = l.tipo,
                idplanilla = string.IsNullOrEmpty(l.idplanilla) ? null : l.idplanilla,
                idnumero = string.IsNullOrEmpty(l.idnumero) ? null : l.idnumero,
                estado = string.IsNullOrEmpty(l.estado) ? null : l.estado[0],
                fecha_reg = l.fecha_reg,
                long_reg = string.IsNullOrEmpty(l.long_reg) ? null : l.long_reg,
                lat_reg = string.IsNullOrEmpty(l.lat_reg) ? null : l.lat_reg,
                imagen_reg = l.imagen_reg,
                gps_cell = string.IsNullOrEmpty(l.gps_cell) ? null : l.gps_cell[0],
                host = string.IsNullOrEmpty(l.host) ? null : l.host,
                dir_ip = string.IsNullOrEmpty(l.dir_ip) ? null : l.dir_ip
            }).ToList();
        }

        public async Task<int> SaveMarcaInAsync(cMarcaIn marca)
        {
            try
            {
                // find existing local record by fecha/hora/tipo (same logic used elsewhere)
                var local = await database.Table<cMarcaInLocal>().FirstOrDefaultAsync(i => i.fecha == marca.fecha && i.hora == marca.hora && i.tipo == marca.tipo && i.idnumero==marca.idnumero);

                if (local is not null)
                {
                    // update all fields, validating nulls
                    local.idtarjeta = string.IsNullOrEmpty(marca.idtarjeta) ? null : marca.idtarjeta;
                    local.idterminal = string.IsNullOrEmpty(marca.idterminal) ? null : marca.idterminal;
                    local.idplanilla = string.IsNullOrEmpty(marca.idplanilla) ? null : marca.idplanilla;
                    local.estado = (marca.estado == null || marca.estado == '\0') ? null : marca.estado.ToString();
                    local.fecha_reg = marca.fecha_reg;
                    local.long_reg = string.IsNullOrEmpty(marca.long_reg) ? null : marca.long_reg;
                    local.lat_reg = string.IsNullOrEmpty(marca.lat_reg) ? null : marca.lat_reg;
                    local.imagen_reg = marca.imagen_reg;
                    local.gps_cell = (marca.gps_cell == null || marca.gps_cell == '\0') ? null : marca.gps_cell.ToString();
                    local.host = string.IsNullOrEmpty(marca.host) ? null : marca.host;
                    local.dir_ip = string.IsNullOrEmpty(marca.dir_ip) ? null : marca.dir_ip;

                    return await database.UpdateAsync(local);
                }
                else
                {
                    var toInsert = new cMarcaInLocal
                    {
                        idtarjeta = string.IsNullOrEmpty(marca.idtarjeta) ? null : marca.idtarjeta,
                        fecha = marca.fecha,
                        hora = string.IsNullOrEmpty(marca.hora) ? null : marca.hora,
                        idterminal = string.IsNullOrEmpty(marca.idterminal) ? null : marca.idterminal,
                        tipo = marca.tipo,
                        idplanilla = string.IsNullOrEmpty(marca.idplanilla) ? null : marca.idplanilla,
                        idnumero = string.IsNullOrEmpty(marca.idnumero) ? null : marca.idnumero,
                        estado = (marca.estado == null || marca.estado == '\0') ? null : marca.estado.ToString(),
                        fecha_reg = marca.fecha_reg,
                        long_reg = string.IsNullOrEmpty(marca.long_reg) ? null : marca.long_reg,
                        lat_reg = string.IsNullOrEmpty(marca.lat_reg) ? null : marca.lat_reg,
                        imagen_reg = marca.imagen_reg,
                        gps_cell = (marca.gps_cell == null || marca.gps_cell == '\0') ? null : marca.gps_cell.ToString(),
                        host = string.IsNullOrEmpty(marca.host) ? null : marca.host,
                        dir_ip = string.IsNullOrEmpty(marca.dir_ip) ? null : marca.dir_ip
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> SaveMarcaInAsync(IEnumerable<cMarcaIn> marcas)
        {
            var marcasList = marcas.ToList();
            if (marcasList.Count == 0) return 0;

            var existingLocals = await database.Table<cMarcaInLocal>().ToListAsync();
            var existingLookup = existingLocals
                .ToLookup(l => (l.fecha, l.hora, l.tipo, l.idnumero));

            var toInsert = new List<cMarcaInLocal>();
            var toUpdate = new List<cMarcaInLocal>();

            foreach (var marca in marcasList)
            {
                var local = existingLookup[(marca.fecha, marca.hora, marca.tipo, marca.idnumero)].FirstOrDefault();

                if (local is not null)
                {
                    local.idtarjeta = string.IsNullOrEmpty(marca.idtarjeta) ? null : marca.idtarjeta;
                    local.idterminal = string.IsNullOrEmpty(marca.idterminal) ? null : marca.idterminal;
                    local.idplanilla = string.IsNullOrEmpty(marca.idplanilla) ? null : marca.idplanilla;
                    local.estado = (marca.estado == null || marca.estado == '\0') ? null : marca.estado.ToString();
                    local.fecha_reg = marca.fecha_reg;
                    local.long_reg = string.IsNullOrEmpty(marca.long_reg) ? null : marca.long_reg;
                    local.lat_reg = string.IsNullOrEmpty(marca.lat_reg) ? null : marca.lat_reg;
                    local.imagen_reg = marca.imagen_reg;
                    local.gps_cell = (marca.gps_cell == null || marca.gps_cell == '\0') ? null : marca.gps_cell.ToString();
                    local.host = string.IsNullOrEmpty(marca.host) ? null : marca.host;
                    local.dir_ip = string.IsNullOrEmpty(marca.dir_ip) ? null : marca.dir_ip;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cMarcaInLocal
                    {
                        idtarjeta = string.IsNullOrEmpty(marca.idtarjeta) ? null : marca.idtarjeta,
                        fecha = marca.fecha,
                        hora = string.IsNullOrEmpty(marca.hora) ? null : marca.hora,
                        idterminal = string.IsNullOrEmpty(marca.idterminal) ? null : marca.idterminal,
                        tipo = marca.tipo,
                        idplanilla = string.IsNullOrEmpty(marca.idplanilla) ? null : marca.idplanilla,
                        idnumero = string.IsNullOrEmpty(marca.idnumero) ? null : marca.idnumero,
                        estado = (marca.estado == null || marca.estado == '\0') ? null : marca.estado.ToString(),
                        fecha_reg = marca.fecha_reg,
                        long_reg = string.IsNullOrEmpty(marca.long_reg) ? null : marca.long_reg,
                        lat_reg = string.IsNullOrEmpty(marca.lat_reg) ? null : marca.lat_reg,
                        imagen_reg = marca.imagen_reg,
                        gps_cell = (marca.gps_cell == null || marca.gps_cell == '\0') ? null : marca.gps_cell.ToString(),
                        host = string.IsNullOrEmpty(marca.host) ? null : marca.host,
                        dir_ip = string.IsNullOrEmpty(marca.dir_ip) ? null : marca.dir_ip
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public async Task<ParametroSinc> GetParametroAsync(int id)
        {
            var l = await database.Table<ParametroSincLocal>()
                            .FirstOrDefaultAsync(i => i.Id == id);
            if (l is null) return null;
            return new ParametroSinc
            {
                Id = l.Id,
                UltimaSincronización = l.UltimaSincronización!
            };
        }

        public async Task<int> SaveParametroAsync(ParametroSinc parametro)
        {
            try
            {
                var model = await GetParametroAsync(parametro.Id);

                if (model is not null)
                {
                    model.UltimaSincronización = DateTime.Now.ToString("yyyyMMdd");

                    return await database.UpdateAsync(model);
                }
                else
                {

                    parametro.Id = 1;
                    var toInsert = new ParametroSincLocal { Id = parametro.Id, UltimaSincronización = parametro.UltimaSincronización };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<cPh_Periodos> GetPeriodoVigenteEmpleado(string idnumero, DateTime fechaPeriodo)
        {
            cPh_Periodos? periodo = new cPh_Periodos() {
                idperiodo = "",
                inicio = new DateTime(DateTime.Now.Year,DateTime.Now.Month, DateTime.Now.Day),
                fin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.AddDays(1).Day),
            };

            try
            {
                
                var periodosLocal = await database.Table<cPh_PeriodosLocal>().ToListAsync();
                var planillasLocal = await database.Table<cPh_PlanillaLocal>().ToListAsync();
                var empleadosLocal = await database.Table<cEmpleadoLocal>().ToListAsync();

                var periodos = periodosLocal.Select(a => new cPh_Periodos
                {
                    idperiodo = a.idperiodo,
                    inicio = a.inicio,
                    fin = a.fin,
                    estado = string.IsNullOrEmpty(a.estado) ? '\0' : a.estado[0],
                    tipo_planilla = a.tipo_planilla![0]
                }).ToList();

                var planillas = planillasLocal.Select(b => new cPh_Planilla
                {
                    idplanilla = b.idplanilla,
                    planilla = b.planilla,
                    nom_conector = b.nom_conector,
                    tipo_planilla = b.tipo_planilla![0]
                }).ToList();

                var empleados = empleadosLocal.Select(c => new cEmpleado
                {
                    IdNumero = c.IdNumero,
                    Estado = string.IsNullOrEmpty(c.Estado) ? '\0' : c.Estado[0],
                    Nombre = c.Nombre,
                    Identificacion = c.Identificacion,
                    IdCCosto = c.IdCCosto,
                    IdPlanilla = c.IdPlanilla,
                    IdDepartamento = c.IdDepartamento,
                    puesto = c.puesto
                }).ToList();

                periodo = (from a in periodos
                           join b in planillas on a.tipo_planilla equals b.tipo_planilla
                           join c in empleados on b.idplanilla equals c.IdPlanilla
                           where c.IdNumero == idnumero
                             && fechaPeriodo >= a.inicio && fechaPeriodo <= a.fin
                           select a).FirstOrDefault();
            }
            catch (Exception)
            {
   
            }
            return periodo!;


        }

        public async Task<List<cPh_Compania>> GetPhCompania()
        {
            var locals = await database.Table<cPh_CompaniaLocal>().ToListAsync();
            var companias = locals.Select(e => new cPh_Compania
            {
                IDCOMP = e.IDCOMP,
                COMPANIA = e.COMPANIA,
                NOM_CONECTOR = e.NOM_CONECTOR,
                STRING_SQL = e.STRING_SQL??"",
                STRING_SQL_ERP = e.STRING_SQL_ERP,
                PAIS = e.PAIS,
                AUTO_PROCESO = e.AUTO_PROCESO![0],
                REMOTE_ERPSERVICE = e.REMOTE_ERPSERVICE,
                MAIL_SERVER = e.MAIL_SERVER ?? "",
                MAIL_USER = e.MAIL_USER?? "",
                MAIL_PASSWORD = e.MAIL_PASSWORD??"",
                MAIL_PORT = e.MAIL_PORT==null?0 : e.MAIL_PORT,
                MAIL_AUTH = e.MAIL_AUTH==null?'F' : e.MAIL_AUTH![0],
                MAIL_SSL = e.MAIL_SSL ==null?'F':e.MAIL_SSL![0],
                HORA_SUP = e.HORA_SUP ?? "",
                HORA_EMP = e.HORA_EMP??"",
                SUPERVISOR_ACUM = e.SUPERVISOR_ACUM![0],
                MAIL_TLS = e.MAIL_TLS![0],
                IN_MARCAS = e.IN_MARCAS == null ? 'N': e.IN_MARCAS![0],
                HORA_CALC = e.HORA_CALC??"",
                APICLIENTID = e.APICLIENTID!,
                APIUSER = e.APIUSER!,
                APIPASSWORD = e.APIPASSWORD!,
                APIDATABASE = e.APIDATABASE!,
                APIURL = e.APIURL!,
                APIURLERP = e.APIURLERP!,
                APIDATABASEERP = e.APIDATABASEERP!,
                APISCHEMAERP = e.APISCHEMAERP!,
                ZONAHORARIA = e.ZONAHORARIA,
                SINCAUTODEPTO = e.SINCAUTODEPTO![0],
                SINCAUTOCCOSTO = e.SINCAUTOCCOSTO![0],
                SINCAUTOPUESTO = e.SINCAUTOPUESTO![0],
                SINCAUTONOMINA = e.SINCAUTONOMINA![0],
                SINCAUTOEMPLEADO = e.SINCAUTOEMPLEADO![0]
            }).ToList();
            return companias;
        }
        public async Task<cPh_Compania> GetPhCompania(string Id)
        {
            var e = await database.Table<cPh_CompaniaLocal>()
                            .FirstOrDefaultAsync(i => i.IDCOMP == Id);
            if (e is null) return null;
            return new cPh_Compania
            {
                IDCOMP = e.IDCOMP,
                COMPANIA = e.COMPANIA,
                NOM_CONECTOR = e.NOM_CONECTOR,
                STRING_SQL = e.STRING_SQL ?? "",
                STRING_SQL_ERP = e.STRING_SQL_ERP,
                PAIS = e.PAIS,
                AUTO_PROCESO = e.AUTO_PROCESO![0],
                REMOTE_ERPSERVICE = e.REMOTE_ERPSERVICE,
                MAIL_SERVER = e.MAIL_SERVER ?? "",
                MAIL_USER = e.MAIL_USER ?? "",
                MAIL_PASSWORD = e.MAIL_PASSWORD ?? "",
                MAIL_PORT = e.MAIL_PORT == null ? 0 : e.MAIL_PORT,
                MAIL_AUTH = e.MAIL_AUTH == null ? 'F' : e.MAIL_AUTH![0],
                MAIL_SSL = e.MAIL_SSL == null ? 'F' : e.MAIL_SSL![0],
                HORA_SUP = e.HORA_SUP ?? "",
                HORA_EMP = e.HORA_EMP ?? "",
                SUPERVISOR_ACUM = e.SUPERVISOR_ACUM![0],
                MAIL_TLS = e.MAIL_TLS![0],
                IN_MARCAS = e.IN_MARCAS == null ? 'N' : e.IN_MARCAS![0],
                HORA_CALC = e.HORA_CALC ?? "",
                APICLIENTID = e.APICLIENTID!,
                APIUSER = e.APIUSER!,
                APIPASSWORD = e.APIPASSWORD!,
                APIDATABASE = e.APIDATABASE!,
                APIURL = e.APIURL!,
                APIURLERP = e.APIURLERP!,
                APIDATABASEERP = e.APIDATABASEERP!,
                APISCHEMAERP = e.APISCHEMAERP!,
                ZONAHORARIA = e.ZONAHORARIA,
                SINCAUTODEPTO = e.SINCAUTODEPTO![0],
                SINCAUTOCCOSTO = e.SINCAUTOCCOSTO![0],
                SINCAUTOPUESTO = e.SINCAUTOPUESTO![0],
                SINCAUTONOMINA = e.SINCAUTONOMINA![0],
                SINCAUTOEMPLEADO = e.SINCAUTOEMPLEADO![0]
            };
        }
        public async Task<int> SavePhCompaniaAsync(cPh_Compania compania)
        {
            try
            {
                var local = await database.Table<cPh_CompaniaLocal>().FirstOrDefaultAsync(i => i.IDCOMP == compania.IDCOMP);

                await database.DeleteAsync(local);

                local = null;

                if (local is not null)
                {
                    local.COMPANIA = compania.COMPANIA;
                    local.NOM_CONECTOR = compania.NOM_CONECTOR;
                    local.STRING_SQL = compania.STRING_SQL;
                    local.STRING_SQL_ERP = compania.STRING_SQL_ERP == null ? null : compania.STRING_SQL_ERP;
                    local.PAIS = compania.PAIS;
                    local.AUTO_PROCESO = compania.AUTO_PROCESO == null ? null : compania.AUTO_PROCESO.ToString();
                    local.REMOTE_ERPSERVICE = compania.REMOTE_ERPSERVICE;
                    local.MAIL_SERVER = compania.MAIL_SERVER;
                    local.MAIL_USER = compania.MAIL_USER;
                    local.MAIL_PASSWORD = compania.MAIL_PASSWORD;
                    local.MAIL_PORT = compania.MAIL_PORT;
                    local.MAIL_AUTH = compania.MAIL_AUTH == null ? null : compania.MAIL_AUTH.ToString();
                    local.MAIL_SSL = compania.MAIL_SSL == null ? null : compania.MAIL_SSL.ToString();
                    local.HORA_SUP = compania.HORA_SUP;
                    local.HORA_EMP = compania.HORA_EMP;
                    local.SUPERVISOR_ACUM = compania.SUPERVISOR_ACUM == null ? null : compania.SUPERVISOR_ACUM.ToString();
                    local.MAIL_TLS = compania.MAIL_TLS == null ? null : compania.MAIL_TLS.ToString();
                    local.HORA_CALC = compania.HORA_CALC;
                    local.IN_MARCAS = compania.IN_MARCAS == null ? null : compania.IN_MARCAS.ToString();
                    local.APICLIENTID = compania.APICLIENTID!;
                    local.APIUSER = compania.APIUSER!;
                    local.APIPASSWORD = compania.APIPASSWORD!;
                    local.APIURL = compania.APIURL!;
                    local.APIDATABASE = compania.APIDATABASE!;
                    local.APIDATABASEERP = compania.APIDATABASEERP!;
                    local.APISCHEMAERP = compania.APISCHEMAERP!;
                    local.APIURLERP =   compania.APIURLERP!;
                    local.ZONAHORARIA = compania.ZONAHORARIA;
                    local.SINCAUTODEPTO = compania.SINCAUTODEPTO!.ToString()!;
                    local.SINCAUTOCCOSTO = compania.SINCAUTOCCOSTO!.ToString()!;
                    local.SINCAUTOPUESTO = compania.SINCAUTOPUESTO!.ToString();
                    local.SINCAUTONOMINA = compania.SINCAUTONOMINA.ToString();
                    local.SINCAUTOEMPLEADO = compania.SINCAUTOEMPLEADO.ToString();

                    return await database.UpdateAsync(local);
                }
                else
                {
                    var toInsert = new cPh_CompaniaLocal
                    {
                        IDCOMP = compania.IDCOMP,
                        COMPANIA = compania.COMPANIA,
                        NOM_CONECTOR = compania.NOM_CONECTOR,
                        STRING_SQL = compania.STRING_SQL,
                        STRING_SQL_ERP = compania.STRING_SQL_ERP == null ? null : compania.STRING_SQL_ERP,
                        PAIS = compania.PAIS,
                        AUTO_PROCESO = compania.AUTO_PROCESO == null ? null : compania.AUTO_PROCESO.ToString(),
                        REMOTE_ERPSERVICE = compania.REMOTE_ERPSERVICE,
                        MAIL_SERVER = compania.MAIL_SERVER,
                        MAIL_USER = compania.MAIL_USER,
                        MAIL_PASSWORD = compania.MAIL_PASSWORD,
                        MAIL_PORT = compania.MAIL_PORT,
                        MAIL_AUTH = compania.MAIL_AUTH == null ? null : compania.MAIL_AUTH.ToString(),
                        MAIL_SSL = compania.MAIL_SSL == null ? null : compania.MAIL_SSL.ToString(),
                        HORA_SUP = compania.HORA_SUP,
                        HORA_EMP = compania.HORA_EMP,
                        SUPERVISOR_ACUM = compania.SUPERVISOR_ACUM == null ? null : compania.SUPERVISOR_ACUM.ToString(),
                        MAIL_TLS = compania.MAIL_TLS == null ? null : compania.MAIL_TLS.ToString(),
                        HORA_CALC = compania.HORA_CALC,
                        IN_MARCAS = compania.IN_MARCAS == null ? null : compania.IN_MARCAS.ToString(),
                        APICLIENTID = compania.APICLIENTID!,
                        APIUSER = compania.APIUSER!,
                        APIPASSWORD = compania.APIPASSWORD!,
                        APIURL = compania.APIURL!,
                        APIDATABASE = compania.APIDATABASE!,
                        APIDATABASEERP = compania.APIDATABASEERP!,
                        APISCHEMAERP = compania.APISCHEMAERP!,
                        APIURLERP = compania.APIURLERP!,
                        ZONAHORARIA = compania.ZONAHORARIA,
                        SINCAUTODEPTO = compania.SINCAUTODEPTO!.ToString()!,
                        SINCAUTOCCOSTO = compania.SINCAUTOCCOSTO!.ToString()!,
                        SINCAUTOPUESTO = compania.SINCAUTOPUESTO!.ToString(),
                        SINCAUTONOMINA = compania.SINCAUTONOMINA.ToString(),
                        SINCAUTOEMPLEADO = compania.SINCAUTOEMPLEADO.ToString(),
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> SavePhCompaniaAsync(IEnumerable<cPh_Compania> companias)
        {
            var companiasList = companias.ToList();
            if (companiasList.Count == 0) return 0;

            var existingLocals = await database.Table<cPh_CompaniaLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => l.IDCOMP);

            var toInsert = new List<cPh_CompaniaLocal>();
            var toUpdate = new List<cPh_CompaniaLocal>();

            foreach (var compania in companiasList)
            {
                if (existingDict.TryGetValue(compania.IDCOMP, out var local))
                {
                    local.COMPANIA = compania.COMPANIA;
                    local.NOM_CONECTOR = compania.NOM_CONECTOR;
                    local.STRING_SQL = compania.STRING_SQL;
                    local.STRING_SQL_ERP = compania.STRING_SQL_ERP == null ? null : compania.STRING_SQL_ERP;
                    local.PAIS = compania.PAIS;
                    local.AUTO_PROCESO = compania.AUTO_PROCESO == null ? null : compania.AUTO_PROCESO.ToString();
                    local.REMOTE_ERPSERVICE = compania.REMOTE_ERPSERVICE;
                    local.MAIL_SERVER = compania.MAIL_SERVER;
                    local.MAIL_USER = compania.MAIL_USER;
                    local.MAIL_PASSWORD = compania.MAIL_PASSWORD;
                    local.MAIL_PORT = compania.MAIL_PORT;
                    local.MAIL_AUTH = compania.MAIL_AUTH == null ? null : compania.MAIL_AUTH.ToString();
                    local.MAIL_SSL = compania.MAIL_SSL == null ? null : compania.MAIL_SSL.ToString();
                    local.HORA_SUP = compania.HORA_SUP;
                    local.HORA_EMP = compania.HORA_EMP;
                    local.SUPERVISOR_ACUM = compania.SUPERVISOR_ACUM == null ? null : compania.SUPERVISOR_ACUM.ToString();
                    local.MAIL_TLS = compania.MAIL_TLS == null ? null : compania.MAIL_TLS.ToString();
                    local.HORA_CALC = compania.HORA_CALC;
                    local.IN_MARCAS = compania.IN_MARCAS == null ? null : compania.IN_MARCAS.ToString();
                    local.APICLIENTID = compania.APICLIENTID!;
                    local.APIUSER = compania.APIUSER!;
                    local.APIPASSWORD = compania.APIPASSWORD!;
                    local.APIURL = compania.APIURL!;
                    local.APIDATABASE = compania.APIDATABASE!;
                    local.APIDATABASEERP = compania.APIDATABASEERP!;
                    local.APISCHEMAERP = compania.APISCHEMAERP!;
                    local.APIURLERP = compania.APIURLERP!;
                    local.ZONAHORARIA = compania.ZONAHORARIA;
                    local.SINCAUTODEPTO = compania.SINCAUTODEPTO!.ToString()!;
                    local.SINCAUTOCCOSTO = compania.SINCAUTOCCOSTO!.ToString()!;
                    local.SINCAUTOPUESTO = compania.SINCAUTOPUESTO!.ToString();
                    local.SINCAUTONOMINA = compania.SINCAUTONOMINA.ToString();
                    local.SINCAUTOEMPLEADO = compania.SINCAUTOEMPLEADO.ToString();
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cPh_CompaniaLocal
                    {
                        IDCOMP = compania.IDCOMP,
                        COMPANIA = compania.COMPANIA,
                        NOM_CONECTOR = compania.NOM_CONECTOR,
                        STRING_SQL = compania.STRING_SQL,
                        STRING_SQL_ERP = compania.STRING_SQL_ERP == null ? null : compania.STRING_SQL_ERP,
                        PAIS = compania.PAIS,
                        AUTO_PROCESO = compania.AUTO_PROCESO == null ? null : compania.AUTO_PROCESO.ToString(),
                        REMOTE_ERPSERVICE = compania.REMOTE_ERPSERVICE,
                        MAIL_SERVER = compania.MAIL_SERVER,
                        MAIL_USER = compania.MAIL_USER,
                        MAIL_PASSWORD = compania.MAIL_PASSWORD,
                        MAIL_PORT = compania.MAIL_PORT,
                        MAIL_AUTH = compania.MAIL_AUTH == null ? null : compania.MAIL_AUTH.ToString(),
                        MAIL_SSL = compania.MAIL_SSL == null ? null : compania.MAIL_SSL.ToString(),
                        HORA_SUP = compania.HORA_SUP,
                        HORA_EMP = compania.HORA_EMP,
                        SUPERVISOR_ACUM = compania.SUPERVISOR_ACUM == null ? null : compania.SUPERVISOR_ACUM.ToString(),
                        MAIL_TLS = compania.MAIL_TLS == null ? null : compania.MAIL_TLS.ToString(),
                        HORA_CALC = compania.HORA_CALC,
                        IN_MARCAS = compania.IN_MARCAS == null ? null : compania.IN_MARCAS.ToString(),
                        APICLIENTID = compania.APICLIENTID!,
                        APIUSER = compania.APIUSER!,
                        APIPASSWORD = compania.APIPASSWORD!,
                        APIURL = compania.APIURL!,
                        APIDATABASE = compania.APIDATABASE!,
                        APIDATABASEERP = compania.APIDATABASEERP!,
                        APISCHEMAERP = compania.APISCHEMAERP!,
                        APIURLERP = compania.APIURLERP!,
                        ZONAHORARIA = compania.ZONAHORARIA,
                        SINCAUTODEPTO = compania.SINCAUTODEPTO!.ToString()!,
                        SINCAUTOCCOSTO = compania.SINCAUTOCCOSTO!.ToString()!,
                        SINCAUTOPUESTO = compania.SINCAUTOPUESTO!.ToString(),
                        SINCAUTONOMINA = compania.SINCAUTONOMINA.ToString(),
                        SINCAUTOEMPLEADO = compania.SINCAUTOEMPLEADO.ToString(),
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public async Task<bool> DeletePhCompaniaAsync()
        {
            await database.DeleteAllAsync<cPh_CompaniaLocal>();
            return true;
        }

        public async Task<bool> DeleteProyectoAsync()
        {
            await database.DeleteAllAsync<cPh_ProyectoLocal>();
            return true;
        }
        public async Task<bool> DeletePhFaseProyectoAsync()
        {
            await database.DeleteAllAsync<cPh_FaseProyectoLocal>();
            return true;
        }

        public async Task<cPortal_Config> GetPortalConfigAsync()
        {
            var l = await database.Table<cPortal_ConfigLocal>()
                            .FirstOrDefaultAsync();
            if (l is null) return null;
            return MapPortalConfigLocalToPortalConfig(l);
        }

        private cPortal_Config MapPortalConfigLocalToPortalConfig(cPortal_ConfigLocal local)
        {
            return new cPortal_Config
            {
                IDAPLICACION = local.IDAPLICACION!,
                IDVERSION = local.IDVERSION,
                IDLICENCIA = local.IDLICENCIA,
                COMPANIA = local.COMPANIA,
                BASEDATOS = local.BASEDATOS,
                PERMANENTE = local.PERMANENTE,  
                ACTIVA = local.ACTIVA,
                REGSITROLIC = local.REGSITROLIC,
                USORESTRINGIDO = local.USORESTRINGIDO,
                USARECONOCIMIENTOFACIAL = local.USARECONOCIMIENTOFACIAL,
                USARGEOLOCALIZACION = local.USARGEOLOCALIZACION,
                LOCALIZACIONREQ = local.LOCALIZACIONREQ,
                AUTOREGISTROROSTRO = local.AUTOREGISTROROSTRO,
                VERLOGMARCAS = local.VERLOGMARCAS,
                MAPAPIKEY = local.MAPAPIKEY,
                FACEDIST = local.FACEDIST,
                FACETEXT = local.FACETEXT,
                ORGANIZACIONBASEID = local.ORGANIZACIONBASEID,
                CANTMAXPLANTILLAS = local.CANTMAXPLANTILLAS,
                VERTARIFA = local.VERTARIFA,
                FECHAULTMODIFICA = local.FECHAULTMODIFICA,
                IDUSUARIOMODIFICA = local.IDUSUARIOMODIFICA,
                USAPROYECTOS = local.USAPROYECTOS,
                TIPOIDENTIFICADOR = local.TIPOIDENTIFICADOR,
            };
        }
        public async Task<int> SavePortalConfigAsync(cPortal_Config config)
        {
            try
            {
                var local = await database.Table<cPortal_ConfigLocal>().FirstOrDefaultAsync(i => i.IDAPLICACION == config.IDAPLICACION);

                if (local is not null)
                {
                    // Actualizar registro existente
                    local.IDVERSION = config.IDVERSION;
                    local.IDLICENCIA = config.IDLICENCIA;
                    local.COMPANIA = config.COMPANIA;
                    local.BASEDATOS = config.BASEDATOS;
                    local.PERMANENTE = config.PERMANENTE;
                    local.ACTIVA = config.ACTIVA;
                    local.REGSITROLIC = config.REGSITROLIC;
                    local.USORESTRINGIDO = config.USORESTRINGIDO;
                    local.USARECONOCIMIENTOFACIAL = config.USARECONOCIMIENTOFACIAL;
                    local.USARGEOLOCALIZACION = config.USARGEOLOCALIZACION;
                    local.LOCALIZACIONREQ = config.LOCALIZACIONREQ;
                    local.AUTOREGISTROROSTRO = config.AUTOREGISTROROSTRO;
                    local.VERLOGMARCAS = config.VERLOGMARCAS;
                    local.MAPAPIKEY = config.MAPAPIKEY;
                    local.FACEDIST = config.FACEDIST;
                    local.FACETEXT = config.FACETEXT;
                    local.ORGANIZACIONBASEID = config.ORGANIZACIONBASEID;
                    local.CANTMAXPLANTILLAS = config.CANTMAXPLANTILLAS;
                    local.VERTARIFA = config.VERTARIFA;
                    local.FECHAULTMODIFICA = config.FECHAULTMODIFICA;
                    local.IDUSUARIOMODIFICA = config.IDUSUARIOMODIFICA;
                    local.USAPROYECTOS = config.USAPROYECTOS;
                    local.TIPOIDENTIFICADOR = config.TIPOIDENTIFICADOR;

                    return await database.UpdateAsync(local);
                }
                else
                {
                    // Insertar nuevo registro
                    var toInsert = new cPortal_ConfigLocal
                    {
                        IDAPLICACION = config.IDAPLICACION,
                        IDVERSION = config.IDVERSION,
                        IDLICENCIA = config.IDLICENCIA,
                        COMPANIA = config.COMPANIA,
                        BASEDATOS = config.BASEDATOS,
                        PERMANENTE = config.PERMANENTE,
                        ACTIVA = config.ACTIVA,
                        REGSITROLIC = config.REGSITROLIC,
                        USORESTRINGIDO = config.USORESTRINGIDO,
                        USARECONOCIMIENTOFACIAL = config.USARECONOCIMIENTOFACIAL,
                        USARGEOLOCALIZACION = config.USARGEOLOCALIZACION,
                        LOCALIZACIONREQ = config.LOCALIZACIONREQ,
                        AUTOREGISTROROSTRO = config.AUTOREGISTROROSTRO,
                        VERLOGMARCAS = config.VERLOGMARCAS,
                        MAPAPIKEY = config.MAPAPIKEY,
                        FACEDIST = config.FACEDIST,
                        FACETEXT = config.FACETEXT,
                        ORGANIZACIONBASEID = config.ORGANIZACIONBASEID,
                        CANTMAXPLANTILLAS = config.CANTMAXPLANTILLAS,
                        VERTARIFA = config.VERTARIFA,
                        FECHAULTMODIFICA = config.FECHAULTMODIFICA,
                        IDUSUARIOMODIFICA = config.IDUSUARIOMODIFICA,
                        USAPROYECTOS = config.USAPROYECTOS,
                        TIPOIDENTIFICADOR = config.TIPOIDENTIFICADOR,
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<NavItemLocal>> GetNavItemLocal(string usuario) 
        { 
            return await database.Table<NavItemLocal>().Where(e=>e.Usuario==usuario).ToListAsync();
        }
        public async Task<int> SaveNavItemAsync(IEnumerable<NavItemLocal> navItems)
        {
            var navItemsList = navItems.ToList();
            if (navItemsList.Count == 0) return 0;

            var existingLocals = await database.Table<NavItemLocal>().ToListAsync();
            var existingLookup = existingLocals.ToLookup(l => (l.Id, l.Usuario));

            var toInsert = new List<NavItemLocal>();
            var toUpdate = new List<NavItemLocal>();

            foreach (var navItem in navItemsList)
            {
                var local = existingLookup[(navItem.Id, navItem.Usuario)].FirstOrDefault();

                if (local is not null)
                {
                    local.Href = navItem.Href;
                    local.IconName = navItem.IconName;
                    local.Text = navItem.Text;
                    local.ParentId = navItem.ParentId;
                    local.IconColor = navItem.IconColor;
                    local.Level = navItem.Level;
                    local.Sequence = navItem.Sequence;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(navItem);
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public cPh_Proyecto MapProyectoLocalToProyecto(cPh_ProyectoLocal local)
        {
            return new cPh_Proyecto
            {
                PROYECTO = local.PROYECTO,
                CENTRO_COSTO = local.CENTRO_COSTO,
                DESCRIPCION = local.DESCRIPCION,

            };
        }
        public async Task<List<cPh_Proyecto>> GetPhProyectoAsync()
        {
            var locals = await database.Table<cPh_ProyectoLocal>().ToListAsync();
            return locals.Select(l => new cPh_Proyecto
            {
                PROYECTO = l.PROYECTO,
                CENTRO_COSTO = l.CENTRO_COSTO,
                DESCRIPCION = l.DESCRIPCION,                

            }).ToList();
        }
        public async Task<cPh_Proyecto> GetPhProyectoAsync(string id)
        {
            var l = await database.Table<cPh_ProyectoLocal>()
                            .FirstOrDefaultAsync(i => i.PROYECTO == id);
            if (l is null) return null;
            return new cPh_Proyecto
            {
                PROYECTO = l.PROYECTO,
                CENTRO_COSTO = l.CENTRO_COSTO,
                DESCRIPCION = l.DESCRIPCION,
                
            };
        }
        public async Task<int> SaveProyectoAsync(cPh_Proyecto proyecto)
        {
            try
            {
                var local = await database.Table<cPh_ProyectoLocal>().FirstOrDefaultAsync(i => i.PROYECTO == proyecto.PROYECTO);

                if (local is not null)
                {
                    // Actualizar registro existente
                    local.CENTRO_COSTO = proyecto.CENTRO_COSTO;
                    local.DESCRIPCION = proyecto.DESCRIPCION;

                    return await database.UpdateAsync(local);
                }
                else
                {
                    // Insertar nuevo registro
                    var toInsert = new cPh_ProyectoLocal
                    {
                        PROYECTO = proyecto.PROYECTO,
                        CENTRO_COSTO = proyecto.CENTRO_COSTO,
                        DESCRIPCION = proyecto.DESCRIPCION,                        
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> SaveProyectoAsync(IEnumerable<cPh_Proyecto> proyectos)
        {
            await DeleteProyectoAsync();

            var proyectosList = proyectos.ToList();
            if (proyectosList.Count == 0) return 0;

            var existingLocals = await database.Table<cPh_ProyectoLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => l.PROYECTO);

            var toInsert = new List<cPh_ProyectoLocal>();
            var toUpdate = new List<cPh_ProyectoLocal>();

            foreach (var proyecto in proyectosList)
            {
                if (existingDict.TryGetValue(proyecto.PROYECTO, out var local))
                {
                    local.CENTRO_COSTO = proyecto.CENTRO_COSTO;
                    local.DESCRIPCION = proyecto.DESCRIPCION;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cPh_ProyectoLocal
                    {
                        PROYECTO = proyecto.PROYECTO,
                        CENTRO_COSTO = proyecto.CENTRO_COSTO,
                        DESCRIPCION = proyecto.DESCRIPCION,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public cPh_FaseProyecto MapProyectoFaseLocalToProyectoFase(cPh_FaseProyectoLocal local)
        {
            return new cPh_FaseProyecto
            {
                PROYECTO = local.PROYECTO,
                NOMBRE = local.NOMBRE,
                DESCRIPCION = local.DESCRIPCION,
                ACEPTA_DATOS = local.ACEPTA_DATOS[0]!,
                FASE = local.FASE,

            };
        }
        public async Task<List<cPh_FaseProyecto>> GetPhFaseProyectoAsync()
        {
            var locals = await database.Table<cPh_FaseProyectoLocal>().ToListAsync();
            return locals.Select(l => new cPh_FaseProyecto
            {
                PROYECTO = l.PROYECTO,
                NOMBRE = l.NOMBRE,
                DESCRIPCION = l.DESCRIPCION,
                ACEPTA_DATOS = l.ACEPTA_DATOS[0]!,
                FASE = l.FASE,  

            }).ToList();
        }
        public async Task<List<cPh_FaseProyecto>> GetPhFaseProyectoAsync(string idProyecto)
        {
            var locals = await database.Table<cPh_FaseProyectoLocal>()
                .Where(e=>e.PROYECTO == idProyecto)
                .ToListAsync();
            return locals.Select(l => new cPh_FaseProyecto
            {
                PROYECTO = l.PROYECTO,
                NOMBRE = l.NOMBRE,
                DESCRIPCION = l.DESCRIPCION,
                ACEPTA_DATOS = l.ACEPTA_DATOS[0]!,
                FASE = l.FASE,
            }).ToList();
        }
        public async Task<cPh_FaseProyecto> GetPhFaseProyectoAsync(string idProyecto, string idfase)
        {
            var l = await database.Table<cPh_FaseProyectoLocal>()
                            .FirstOrDefaultAsync(i => i.PROYECTO == idProyecto && i.FASE == idfase);
            if (l is null) return null;
            return new cPh_FaseProyecto
            {
                PROYECTO = l.PROYECTO,
                NOMBRE = l.NOMBRE,
                DESCRIPCION = l.DESCRIPCION,
                ACEPTA_DATOS = l.ACEPTA_DATOS[0]!,
                FASE = l.FASE,
            };
        }
        public async Task<int> SaveFaseProyectoAsync(cPh_FaseProyecto faseProyecto)
        {
            try
            {
                var local = await database.Table<cPh_FaseProyectoLocal>().FirstOrDefaultAsync(i => i.PROYECTO == faseProyecto.PROYECTO && i.FASE == faseProyecto.FASE);

                if (local is not null)
                {
                    // Actualizar registro existente
                    local.NOMBRE = faseProyecto.NOMBRE;
                    local.DESCRIPCION = faseProyecto.DESCRIPCION;
                    local.ACEPTA_DATOS = faseProyecto.ACEPTA_DATOS.ToString();

                    return await database.UpdateAsync(local);
                }
                else
                {
                    // Insertar nuevo registro
                    var toInsert = new cPh_FaseProyectoLocal
                    {
                        PROYECTO = faseProyecto.PROYECTO,
                        FASE = faseProyecto.FASE,
                        NOMBRE = faseProyecto.NOMBRE,
                        DESCRIPCION = faseProyecto.DESCRIPCION,
                        ACEPTA_DATOS = faseProyecto.ACEPTA_DATOS.ToString(),
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> SaveFaseProyectoAsync(IEnumerable<cPh_FaseProyecto> fasesProyecto)
        {
            await DeletePhFaseProyectoAsync();
            var fasesList = fasesProyecto.ToList();
            if (fasesList.Count == 0) return 0;

            var existingLocals = await database.Table<cPh_FaseProyectoLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => (l.PROYECTO, l.FASE));

            var toInsert = new List<cPh_FaseProyectoLocal>();
            var toUpdate = new List<cPh_FaseProyectoLocal>();

            foreach (var fase in fasesList)
            {
                if (existingDict.TryGetValue((fase.PROYECTO, fase.FASE), out var local))
                {
                    local.NOMBRE = fase.NOMBRE;
                    local.DESCRIPCION = fase.DESCRIPCION;
                    local.ACEPTA_DATOS = fase.ACEPTA_DATOS.ToString();
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cPh_FaseProyectoLocal
                    {
                        PROYECTO = fase.PROYECTO,
                        FASE = fase.FASE,
                        NOMBRE = fase.NOMBRE,
                        DESCRIPCION = fase.DESCRIPCION,
                        ACEPTA_DATOS = fase.ACEPTA_DATOS.ToString(),
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public cCentroCosto MapCentroCostoLocalToCentroCosto(cCentroCostoLocal local)
        {
            return new cCentroCosto
            {
                IdCCosto = local.IdCCosto,
                Descripcion = local.Descripcion,
                Alias_CCosto = local.Alias_CCosto,
                Distribuye = local.Distribuye![0],

            };
        }
        public async Task<List<cCentroCosto>> GetCentroCostoAsync()
        {
            var locals = await database.Table<cCentroCostoLocal>().ToListAsync();
            return locals.Select(l => new cCentroCosto
            {
                IdCCosto = l.IdCCosto,
                Descripcion = l.Descripcion,
                Alias_CCosto = l.Alias_CCosto,
                Distribuye = l.Distribuye![0],

            }).ToList();
        }
        public async Task<cCentroCosto> GetCentroCostoAsync(string id)
        {
            var l = await database.Table<cCentroCostoLocal>()
                            .FirstOrDefaultAsync(i => i.IdCCosto == id);
            if (l is null) return null;
            return new cCentroCosto
            {
                IdCCosto = l.IdCCosto,
                Descripcion = l.Descripcion,
                Alias_CCosto = l.Alias_CCosto,
                Distribuye = l.Distribuye![0],
            };
        }
        public async Task<int> SaveCentroCostoAsync(cCentroCosto centroCosto)
        {
            try
            {
                var local = await database.Table<cCentroCostoLocal>().FirstOrDefaultAsync(i => i.IdCCosto == centroCosto.IdCCosto);
                if (local is not null)
                {
                    // Actualizar registro existente
                    local.Descripcion = centroCosto.Descripcion;
                    local.Alias_CCosto = centroCosto.Alias_CCosto;
                    local.Distribuye = centroCosto.Distribuye.ToString();

                    return await database.UpdateAsync(local);
                }
                else
                {
                    // Insertar nuevo registro
                    var toInsert = new cCentroCostoLocal
                    {
                        IdCCosto = centroCosto.IdCCosto,
                        Descripcion = centroCosto.Descripcion,
                        Alias_CCosto = centroCosto.Alias_CCosto,
                        Distribuye = centroCosto.Distribuye.ToString(),
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> SaveCentroCostoAsync(IEnumerable<cCentroCosto> centrosCosto)
        {
            var centrosList = centrosCosto.ToList();
            if (centrosList.Count == 0) return 0;

            var existingLocals = await database.Table<cCentroCostoLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => l.IdCCosto!);

            var toInsert = new List<cCentroCostoLocal>();
            var toUpdate = new List<cCentroCostoLocal>();

            foreach (var cc in centrosList)
            {
                if (existingDict.TryGetValue(cc.IdCCosto!, out var local))
                {
                    local.Descripcion = cc.Descripcion;
                    local.Alias_CCosto = cc.Alias_CCosto;
                    local.Distribuye = cc.Distribuye.ToString();
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cCentroCostoLocal
                    {
                        IdCCosto = cc.IdCCosto,
                        Descripcion = cc.Descripcion,
                        Alias_CCosto = cc.Alias_CCosto,
                        Distribuye = cc.Distribuye.ToString(),
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public cMarcaDistribucionConcepto MapMarcaDistribucionConceptoLocalToMarcaDistribucionConcepto(cMarcaDistribucionConceptoLocal local)
        {
            return new cMarcaDistribucionConcepto
            {
                IDREGISTRO = local.IDREGISTRO,
                IDNUMERO = local.IDNUMERO,
                FECHA = local.FECHA,
                INICIO = local.INICIO,
                FIN = local.FIN,
                ESTADO = local.ESTADO![0],
                IDCCOSTO = local.IDCCOSTO,
                PROYECTO = local.PROYECTO,
                FASE = local.FASE,  
                IDPLANILLA = local.IDPLANILLA,
                FECHA_DIST = local.FECHA_DIST,
                IDDIST = local.IDDIST,                
                CANTIDAD = local.CANTIDAD,
                COMENTARIO = local.COMENTARIO,
                LAT_REG = local.LAT_REG,
                LON_REG = local.LON_REG,
                idsolicitud = local.idsolicitud,
            };
        }

        public async Task<List<cMarcaDistribucionConcepto>> GetMarcaDistribucionConceptoAsync()
        {
            var locals = await database.Table<cMarcaDistribucionConceptoLocal>().ToListAsync();
            return locals.Select(l => new cMarcaDistribucionConcepto
            {
                IDREGISTRO = l.IDREGISTRO,
                IDNUMERO = l.IDNUMERO,
                FECHA = l.FECHA,
                INICIO = l.INICIO,
                FIN = l.FIN,
                ESTADO = l.ESTADO![0],
                IDCCOSTO = l.IDCCOSTO,
                PROYECTO = l.PROYECTO,
                FASE = l.FASE,  
                IDPLANILLA = l.IDPLANILLA,
                FECHA_DIST = l.FECHA_DIST,
                IDDIST = l.IDDIST,                
                CANTIDAD = l.CANTIDAD,
                COMENTARIO = l.COMENTARIO,
                LAT_REG = l.LAT_REG,
                LON_REG = l.LON_REG,
                idsolicitud = l.idsolicitud,
            }).ToList();
        }

        public async Task<cMarcaDistribucionConcepto> GetMarcaDistribucionConceptoAsync(long id)
        {
            var l = await database.Table<cMarcaDistribucionConceptoLocal>()
                            .FirstOrDefaultAsync(i => i.IDREGISTRO == id);
            if (l is null) return null;
            return new cMarcaDistribucionConcepto
            {
                IDREGISTRO = l.IDREGISTRO,
                IDNUMERO = l.IDNUMERO,
                FECHA = l.FECHA,
                INICIO = l.INICIO,
                FIN = l.FIN,
                ESTADO = l.ESTADO![0],
                IDCCOSTO = l.IDCCOSTO,
                PROYECTO = l.PROYECTO,
                FASE = l.FASE,  
                IDPLANILLA = l.IDPLANILLA,
                FECHA_DIST = l.FECHA_DIST,
                IDDIST = l.IDDIST,                
                CANTIDAD = l.CANTIDAD,
                COMENTARIO = l.COMENTARIO,
                LAT_REG = l.LAT_REG,
                LON_REG = l.LON_REG,
                idsolicitud = l.idsolicitud,
            };
        }

        public async Task<List<cMarcaDistribucionConcepto>> GetMarcaDistribucionConceptoBySupAsync(string idnumero)
        {
            var locals = await database.Table<cMarcaDistribucionConceptoLocal>()
                         .Where(e=>e.IdUsuario== idnumero 
                                && e.ESTADO == "P").ToListAsync();
            return locals.Select(l => new cMarcaDistribucionConcepto
            {
                IDREGISTRO = l.IDREGISTRO,
                IDNUMERO = l.IDNUMERO,
                FECHA = l.FECHA,
                INICIO = l.INICIO,
                FIN = l.FIN,
                ESTADO = l.ESTADO![0],
                IDCCOSTO = l.IDCCOSTO,
                PROYECTO = l.PROYECTO,
                FASE = l.FASE,
                IDPLANILLA = l.IDPLANILLA,
                FECHA_DIST = l.FECHA_DIST,
                IDDIST = l.IDDIST,
                CANTIDAD = l.CANTIDAD,
                COMENTARIO = l.COMENTARIO,
                LAT_REG = l.LAT_REG,
                LON_REG = l.LON_REG,
                idsolicitud = l.idsolicitud,
            }).ToList();
        }

        public async Task<List<cMarcaDistribucionConcepto>> GetMarcaDistribucionPendienteAsync()
        {
            var locals = await database.Table<cMarcaDistribucionConceptoLocal>()
                         .Where(e =>e.ESTADO == "P").ToListAsync();
            return locals.Select(l => new cMarcaDistribucionConcepto
            {
                IDREGISTRO = l.IDREGISTRO,
                IDNUMERO = l.IDNUMERO,
                FECHA = l.FECHA,
                INICIO = l.INICIO,
                FIN = l.FIN,
                ESTADO = l.ESTADO![0],
                IDCCOSTO = l.IDCCOSTO,
                PROYECTO = l.PROYECTO,
                FASE = l.FASE,
                IDPLANILLA = l.IDPLANILLA,
                FECHA_DIST = l.FECHA_DIST,
                IDDIST = l.IDDIST,
                CANTIDAD = l.CANTIDAD,
                COMENTARIO = l.COMENTARIO,
                LAT_REG = l.LAT_REG,
                LON_REG = l.LON_REG,
                idsolicitud = l.idsolicitud,
            }).ToList();
        }
        public async Task<int> SaveMarcaDistribucionConceptoAsync(cMarcaDistribucionConcepto marca)
        {
            try
            {
                var local = await database.Table<cMarcaDistribucionConceptoLocal>().FirstOrDefaultAsync(i => i.IDREGISTRO == marca.IDREGISTRO);

                if (local is not null)
                {
                    local.ESTADO = marca.ESTADO.ToString();                   
                    return await database.UpdateAsync(local);
                }
                
            }
            catch (Exception)
            {
                throw;
            }
            return 0;
        }

        public async Task<int> SaveMarcaDistribucionConceptoAsync(IEnumerable<cMarcaDistribucionConcepto> marcas, string idUsuario)
        {
            var marcasList = marcas.ToList();
            if (marcasList.Count == 0) return 0;

            var existingLocals = await database.Table<cMarcaDistribucionConceptoLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => l.IDREGISTRO);

            var toInsert = new List<cMarcaDistribucionConceptoLocal>();
            var toUpdate = new List<cMarcaDistribucionConceptoLocal>();

            foreach (var marca in marcasList)
            {
                if (existingDict.TryGetValue(marca.IDREGISTRO, out var local))
                {
                    local.IDNUMERO = marca.IDNUMERO;
                    local.FECHA = marca.FECHA;
                    local.INICIO = marca.INICIO;
                    local.FIN = marca.FIN;
                    local.ESTADO = marca.ESTADO.ToString();
                    local.IDCCOSTO = marca.IDCCOSTO;
                    local.PROYECTO = marca.PROYECTO;
                    local.FASE = marca.FASE;
                    local.IDPLANILLA = marca.IDPLANILLA;
                    local.FECHA_DIST = marca.FECHA_DIST;
                    local.IDDIST = marca.IDDIST;
                    local.CANTIDAD = marca.CANTIDAD;
                    local.COMENTARIO = marca.COMENTARIO;
                    local.LAT_REG = marca.LAT_REG;
                    local.LON_REG = marca.LON_REG;
                    local.idsolicitud = marca.idsolicitud;
                    local.IdUsuario = idUsuario;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cMarcaDistribucionConceptoLocal
                    {
                        IDNUMERO = marca.IDNUMERO,
                        FECHA = marca.FECHA,
                        INICIO = marca.INICIO,
                        FIN = marca.FIN,
                        ESTADO = "P",
                        IDCCOSTO = marca.IDCCOSTO,
                        PROYECTO = marca.PROYECTO,
                        FASE = marca.FASE,
                        IDPLANILLA = marca.IDPLANILLA,
                        FECHA_DIST = marca.FECHA_DIST,
                        IDDIST = marca.IDDIST,
                        CANTIDAD = marca.CANTIDAD,
                        COMENTARIO = marca.COMENTARIO,
                        LAT_REG = marca.LAT_REG,
                        LON_REG = marca.LON_REG,
                        idsolicitud = marca.idsolicitud,
                        IdUsuario = idUsuario,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public async Task<bool> DeleteMarcaDistribucionConceptoAsync(long id)
        {
            var l = await database.Table<cMarcaDistribucionConceptoLocal>()
                            .FirstOrDefaultAsync(i => i.IDREGISTRO == id);
            if (l is null) return false;
            await database.DeleteAsync(l);
            return true;
        }

        public cPh_Usuario MapUsuarioLocalToUsuario(cPh_UsuarioLocal l)
        {
            return new cPh_Usuario
            {
                IDUSUARIO = l.IDUSUARIO,
                PLANILLAS = l.PLANILLAS,
                NIVEL = l.NIVEL,
                GRUPOS = l.GRUPOS,
                ESTADO = string.IsNullOrEmpty(l.ESTADO) ? null : l.ESTADO[0],
                FPERIODO = l.FPERIODO,
                FPLANILLA = l.FPLANILLA,
                TURNOS = l.TURNOS,
                ORDEN_EMP = string.IsNullOrEmpty(l.ORDEN_EMP) ? '\0' : l.ORDEN_EMP[0],
                TIPO_EDT = string.IsNullOrEmpty(l.TIPO_EDT) ? '\0' : l.TIPO_EDT[0],
                FILT_PRGT = string.IsNullOrEmpty(l.FILT_PRGT) ? null : l.FILT_PRGT[0],
                NIVEL_APROB_EXT = l.NIVEL_APROB_EXT,
                FCANT_MESES = l.FCANT_MESES,
                FFECHA_EVALUAR = l.FFECHA_EVALUAR,
                PT_AGRUP = string.IsNullOrEmpty(l.PT_AGRUP) ? null : l.PT_AGRUP[0],
            };
        }

        public async Task<List<cPh_Usuario>> GetPhUsuarioAsync()
        {
            var locals = await database.Table<cPh_UsuarioLocal>().ToListAsync();
            return locals.Select(l => MapUsuarioLocalToUsuario(l)).ToList();
        }

        public async Task<cPh_Usuario> GetPhUsuarioAsync(int id)
        {
            var l = await database.Table<cPh_UsuarioLocal>()
                            .FirstOrDefaultAsync(i => i.IDUSUARIO == id);
            if (l is null) return null;
            return MapUsuarioLocalToUsuario(l);
        }

        public async Task<int> SavePhUsuarioAsync(cPh_Usuario usuario, string codigo)
        {
            try
            {
                var local = await database.Table<cPh_UsuarioLocal>().FirstOrDefaultAsync(i => i.IDUSUARIO == usuario.IDUSUARIO);

                if (local is not null)
                {
                    local.PLANILLAS = usuario.PLANILLAS;
                    local.NIVEL = usuario.NIVEL;
                    local.GRUPOS = usuario.GRUPOS;
                    local.ESTADO = usuario.ESTADO == null ? null : usuario.ESTADO.ToString();
                    local.FPERIODO = usuario.FPERIODO;
                    local.FPLANILLA = usuario.FPLANILLA;
                    local.TURNOS = usuario.TURNOS;
                    local.ORDEN_EMP = usuario.ORDEN_EMP.ToString();
                    local.TIPO_EDT = usuario.TIPO_EDT.ToString();
                    local.FILT_PRGT = usuario.FILT_PRGT == null ? null : usuario.FILT_PRGT.ToString();
                    local.NIVEL_APROB_EXT = usuario.NIVEL_APROB_EXT;
                    local.FCANT_MESES = usuario.FCANT_MESES;
                    local.FFECHA_EVALUAR = usuario.FFECHA_EVALUAR;
                    local.PT_AGRUP = usuario.PT_AGRUP == null ? null : usuario.PT_AGRUP.ToString();
                    local.CODIGO = codigo;

                    return await database.UpdateAsync(local);
                }
                else
                {
                    var toInsert = new cPh_UsuarioLocal
                    {
                        IDUSUARIO = usuario.IDUSUARIO,
                        PLANILLAS = usuario.PLANILLAS,
                        NIVEL = usuario.NIVEL,
                        GRUPOS = usuario.GRUPOS,
                        ESTADO = usuario.ESTADO == null ? null : usuario.ESTADO.ToString(),
                        FPERIODO = usuario.FPERIODO,
                        FPLANILLA = usuario.FPLANILLA,
                        TURNOS = usuario.TURNOS,
                        ORDEN_EMP = usuario.ORDEN_EMP.ToString(),
                        TIPO_EDT = usuario.TIPO_EDT.ToString(),
                        FILT_PRGT = usuario.FILT_PRGT == null ? null : usuario.FILT_PRGT.ToString(),
                        NIVEL_APROB_EXT = usuario.NIVEL_APROB_EXT,
                        FCANT_MESES = usuario.FCANT_MESES,
                        FFECHA_EVALUAR = usuario.FFECHA_EVALUAR,
                        PT_AGRUP = usuario.PT_AGRUP == null ? null : usuario.PT_AGRUP.ToString(),
                        CODIGO = codigo,
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> SavePhUsuarioAsync(IEnumerable<cPh_Usuario> usuarios)
        {
            var usuariosList = usuarios.ToList();
            if (usuariosList.Count == 0) return 0;

            var existingLocals = await database.Table<cPh_UsuarioLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => l.IDUSUARIO);

            var toInsert = new List<cPh_UsuarioLocal>();
            var toUpdate = new List<cPh_UsuarioLocal>();

            foreach (var usuario in usuariosList)
            {
                if (existingDict.TryGetValue(usuario.IDUSUARIO, out var local))
                {
                    local.PLANILLAS = usuario.PLANILLAS;
                    local.NIVEL = usuario.NIVEL;
                    local.GRUPOS = usuario.GRUPOS;
                    local.ESTADO = usuario.ESTADO == null ? null : usuario.ESTADO.ToString();
                    local.FPERIODO = usuario.FPERIODO;
                    local.FPLANILLA = usuario.FPLANILLA;
                    local.TURNOS = usuario.TURNOS;
                    local.ORDEN_EMP = usuario.ORDEN_EMP.ToString();
                    local.TIPO_EDT = usuario.TIPO_EDT.ToString();
                    local.FILT_PRGT = usuario.FILT_PRGT == null ? null : usuario.FILT_PRGT.ToString();
                    local.NIVEL_APROB_EXT = usuario.NIVEL_APROB_EXT;
                    local.FCANT_MESES = usuario.FCANT_MESES;
                    local.FFECHA_EVALUAR = usuario.FFECHA_EVALUAR;
                    local.PT_AGRUP = usuario.PT_AGRUP == null ? null : usuario.PT_AGRUP.ToString();
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cPh_UsuarioLocal
                    {
                        IDUSUARIO = usuario.IDUSUARIO,
                        PLANILLAS = usuario.PLANILLAS,
                        NIVEL = usuario.NIVEL,
                        GRUPOS = usuario.GRUPOS,
                        ESTADO = usuario.ESTADO == null ? null : usuario.ESTADO.ToString(),
                        FPERIODO = usuario.FPERIODO,
                        FPLANILLA = usuario.FPLANILLA,
                        TURNOS = usuario.TURNOS,
                        ORDEN_EMP = usuario.ORDEN_EMP.ToString(),
                        TIPO_EDT = usuario.TIPO_EDT.ToString(),
                        FILT_PRGT = usuario.FILT_PRGT == null ? null : usuario.FILT_PRGT.ToString(),
                        NIVEL_APROB_EXT = usuario.NIVEL_APROB_EXT,
                        FCANT_MESES = usuario.FCANT_MESES,
                        FFECHA_EVALUAR = usuario.FFECHA_EVALUAR,
                        PT_AGRUP = usuario.PT_AGRUP == null ? null : usuario.PT_AGRUP.ToString(),
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public async Task<IEnumerable<cPh_Proyecto>> GetProyectoByCCostoAsync(string idccosto)
        {
            var locals = await database.Table<cPh_ProyectoLocal>()
                            .Where(e => e.CENTRO_COSTO == idccosto)
                            .ToListAsync();
            return locals.Select(l => MapProyectoLocalToProyecto(l)).ToList();
        }

        public async Task<IEnumerable<cPh_FaseProyecto>> GetFaseProyectoAsync(string idProyecto)
        {
            var locals = await database.Table<cPh_FaseProyectoLocal>()
                            .Where(e => e.PROYECTO == idProyecto)
                            .ToListAsync();
            return locals.Select(l => MapProyectoFaseLocalToProyectoFase(l)).ToList();
        }

        public cTemplateFACE MapTemplateFACELocalToTemplateFACE(cTemplateFACELocal local)
        {
            return new cTemplateFACE
            {
                IDNUMERO = local.IDNUMERO,
                INDEXID = local.INDEXID,
                FACETEMPLATE = local.FACETEMPLATE,
            };
        }

        public async Task<List<cTemplateFACE>> GetTemplateFACEAsync()
        {
            var locals = await database.Table<cTemplateFACELocal>().ToListAsync();
            return locals.Select(l => MapTemplateFACELocalToTemplateFACE(l)).ToList();
        }

        public async Task<List<cTemplateFACE>> GetTemplateFACEAsync(string idnumero)
        {
            var locals = await database.Table<cTemplateFACELocal>()
                            .Where(e => e.IDNUMERO == idnumero)
                            .ToListAsync();
            return locals.Select(l => MapTemplateFACELocalToTemplateFACE(l)).ToList();
        }

        public async Task<int> SaveTemplateFACEAsync(cTemplateFACE template)
        {
            try
            {
                var local = await database.Table<cTemplateFACELocal>()
                    .FirstOrDefaultAsync(i => i.IDNUMERO == template.IDNUMERO && i.INDEXID == template.INDEXID);

                if (local is not null)
                {
                    return await database.ExecuteAsync(
                        "UPDATE cTemplateFACE SET FACETEMPLATE = ? WHERE IDNUMERO = ? AND INDEXID = ?",
                        template.FACETEMPLATE, template.IDNUMERO, template.INDEXID);
                }
                else
                {
                    var toInsert = new cTemplateFACELocal
                    {
                        IDNUMERO = template.IDNUMERO,
                        INDEXID = template.INDEXID,
                        FACETEMPLATE = template.FACETEMPLATE,
                    };
                    return await database.InsertAsync(toInsert);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> SaveTemplateFACEAsync(IEnumerable<cTemplateFACE> templates)
        {
            var templatesList = templates.ToList();
            if (templatesList.Count == 0) return 0;

            var existingLocals = await database.Table<cTemplateFACELocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => (l.IDNUMERO, l.INDEXID));

            var toInsert = new List<cTemplateFACELocal>();
            var toUpdate = new List<cTemplateFACELocal>();

            foreach (var template in templatesList)
            {
                if (existingDict.TryGetValue((template.IDNUMERO, template.INDEXID), out var local))
                {
                    local.FACETEMPLATE = template.FACETEMPLATE;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cTemplateFACELocal
                    {
                        IDNUMERO = template.IDNUMERO,
                        INDEXID = template.INDEXID,
                        FACETEMPLATE = template.FACETEMPLATE,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                foreach (var u in toUpdate)
                {
                    totalAffected += db.Execute(
                        "UPDATE cTemplateFACE SET FACETEMPLATE = ? WHERE IDNUMERO = ? AND INDEXID = ?",
                        u.FACETEMPLATE, u.IDNUMERO, u.INDEXID);
                }
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

        public async Task<bool> DeleteTemplateFACEAsync()
        {
            await database.DeleteAllAsync<cTemplateFACELocal>();
            return true;
        }

        public async Task<bool> DeleteTemplateFACEAsync(string idnumero)
        {
            var locals = await database.Table<cTemplateFACELocal>()
                            .Where(e => e.IDNUMERO == idnumero)
                            .ToListAsync();
            foreach (var local in locals)
            {
                await database.ExecuteAsync(
                    "DELETE FROM cTemplateFACE WHERE IDNUMERO = ? AND INDEXID = ?",
                    local.IDNUMERO, local.INDEXID);
            }
            return true;
        }

        public async Task<cParametroApiLocal> GetParametroApiLocalAsync(int id)
        {
            var l = await database.Table<cParametroApiLocal>()
                            .FirstOrDefaultAsync(i => i.Id == id);
            if (l is null) return null;
            return new cParametroApiLocal
            {
                Id = l.Id,
                ApiGeoTimeUrl = l.ApiGeoTimeUrl,
                ApiGeotimeDataBase = l.ApiGeotimeDataBase,
                ApiGeotimeSchema = l.ApiGeotimeSchema,
                ApiClientId = l.ApiClientId,
                ApiUser = l.ApiUser,
                ApiPassword = l.ApiPassword,
                ApiAuxiliarSchema = l.ApiAuxiliarSchema,
                ApiAuxiliarUrl = l.ApiAuxiliarUrl,
            };
        }

        public async Task<int> SaveParametroApiAsync(cParametroApiLocal parametro)
        {
            try
            {
                var model = await database.Table<cParametroApiLocal>()
                            .FirstOrDefaultAsync(i => i.Id == parametro.Id);

                if (model is not null)
                {
                    model.ApiGeoTimeUrl = parametro.ApiGeoTimeUrl;
                    model.ApiGeotimeDataBase = parametro.ApiGeotimeDataBase;
                    model.ApiGeotimeSchema = parametro.ApiGeotimeSchema;
                    model.ApiClientId = parametro.ApiClientId;
                    model.ApiUser = parametro.ApiUser;
                    model.ApiPassword = parametro.ApiPassword;
                    model.ApiAuxiliarSchema = parametro.ApiAuxiliarSchema;
                    model.ApiAuxiliarUrl = parametro.ApiAuxiliarUrl;

                    return await database.UpdateAsync(model);
                }
                else
                {
                    parametro.Id = 1;
                    return await database.InsertAsync(parametro);
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // TicoFrut — GeoAgrFinca
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<GeoAgrFinca>> GetGeoAgrFincaAsync()
        {
            var locals = await database.Table<GeoAgrFincaLocal>().ToListAsync();
            return locals.Select(l => new GeoAgrFinca
            {
                Compania = l.Compania,
                Pais     = l.Pais,
                Bloque   = l.Bloque,
                Finca    = l.Finca,
            }).ToList();
        }

        public async Task<List<GeoAgrFinca>> GetGeoAgrFincaAsync(string compania, string pais)
        {
            var locals = await database.Table<GeoAgrFincaLocal>()
                .Where(l => l.Compania == compania && l.Pais == pais)
                .ToListAsync();
            return locals.Select(l => new GeoAgrFinca
            {
                Compania = l.Compania,
                Pais     = l.Pais,
                Bloque   = l.Bloque,
                Finca    = l.Finca,
            }).ToList();
        }

        public async Task<GeoAgrFinca?> GetGeoAgrFincaAsync(string compania, string pais, string bloque, string finca)
        {
            var l = await database.Table<GeoAgrFincaLocal>()
                .FirstOrDefaultAsync(i => i.Compania == compania && i.Pais == pais
                                       && i.Bloque == bloque && i.Finca == finca);
            if (l is null) return null;
            return new GeoAgrFinca { Compania = l.Compania, Pais = l.Pais, Bloque = l.Bloque, Finca = l.Finca };
        }

        public async Task<int> SaveGeoAgrFincaAsync(IEnumerable<GeoAgrFinca> fincas)
        {
            var list = fincas.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrFincaLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(l => (l.Compania!, l.Pais!, l.Bloque!, l.Finca!));

            var toInsert = new List<GeoAgrFincaLocal>();
            var toUpdate = new List<GeoAgrFincaLocal>();

            foreach (var f in list)
            {
                if (existingDict.TryGetValue((f.Compania!, f.Pais!, f.Bloque!, f.Finca!), out var local))
                {
                    toUpdate.Add(local); // sin campos mutables; registro ya existe
                }
                else
                {
                    toInsert.Add(new GeoAgrFincaLocal
                    {
                        Compania = f.Compania,
                        Pais     = f.Pais,
                        Bloque   = f.Bloque,
                        Finca    = f.Finca,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        public async Task<bool> DeleteGeoAgrFincaAsync()
        {
            await database.ExecuteAsync("DELETE FROM GeoAgrFinca");
            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // TicoFrut — GeoAgrLote
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<GeoAgrLote>> GetGeoAgrLoteAsync()
        {
            var locals = await database.Table<GeoAgrLoteLocal>().ToListAsync();
            return locals.Select(l => new GeoAgrLote
            {
                Compania  = l.Compania,
                Bloque    = l.Bloque,
                Finca     = l.Finca,
                Lote      = l.Lote,
                Pais      = l.Pais,
                TipoSuelo = l.TipoSuelo,
                Variedad  = l.Variedad,
            }).ToList();
        }

        public async Task<List<GeoAgrLote>> GetGeoAgrLoteAsync(string compania)
        {
            var locals = await database.Table<GeoAgrLoteLocal>()
                .Where(l => l.Compania == compania)
                .ToListAsync();
            return locals.Select(l => new GeoAgrLote
            {
                Compania = l.Compania,
                Bloque = l.Bloque,
                Finca = l.Finca,
                Lote = l.Lote,
                Pais = l.Pais,
                TipoSuelo = l.TipoSuelo,
                Variedad = l.Variedad,
            }).ToList();
        }

        public async Task<List<GeoAgrLote>> GetGeoAgrLoteAsync(string compania, string bloque, string finca)
        {
            var locals = await database.Table<GeoAgrLoteLocal>()
                .Where(l => l.Compania == compania && l.Bloque == bloque && l.Finca == finca)
                .ToListAsync();
            return locals.Select(l => new GeoAgrLote
            {
                Compania  = l.Compania,
                Bloque    = l.Bloque,
                Finca     = l.Finca,
                Lote      = l.Lote,
                Pais      = l.Pais,
                TipoSuelo = l.TipoSuelo,
                Variedad  = l.Variedad,
            }).ToList();
        }

        public async Task<GeoAgrLote?> GetGeoAgrLoteAsync(string compania, string bloque, string finca, string lote)
        {
            var l = await database.Table<GeoAgrLoteLocal>()
                .FirstOrDefaultAsync(i => i.Compania == compania && i.Bloque == bloque
                                       && i.Finca == finca && i.Lote == lote);
            if (l is null) return null;
            return new GeoAgrLote
            {
                Compania  = l.Compania,
                Bloque    = l.Bloque,
                Finca     = l.Finca,
                Lote      = l.Lote,
                Pais      = l.Pais,
                TipoSuelo = l.TipoSuelo,
                Variedad  = l.Variedad,
            };
        }

        public async Task<int> SaveGeoAgrLoteAsync(IEnumerable<GeoAgrLote> lotes)
        {
            var list = lotes.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrLoteLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(l => (l.Compania!, l.Bloque!, l.Finca!, l.Lote!));

            var toInsert = new List<GeoAgrLoteLocal>();
            var toUpdate = new List<GeoAgrLoteLocal>();

            foreach (var item in list)
            {
                if (existingDict.TryGetValue((item.Compania!, item.Bloque!, item.Finca!, item.Lote!), out var local))
                {
                    local.Pais      = item.Pais;
                    local.TipoSuelo = item.TipoSuelo;
                    local.Variedad  = item.Variedad;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new GeoAgrLoteLocal
                    {
                        Compania  = item.Compania,
                        Bloque    = item.Bloque,
                        Finca     = item.Finca,
                        Lote      = item.Lote,
                        Pais      = item.Pais,
                        TipoSuelo = item.TipoSuelo,
                        Variedad  = item.Variedad,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.Execute(
                        "UPDATE GeoAgrLote SET Pais=?, TipoSuelo=?, Variedad=? WHERE Compania=? AND Bloque=? AND Finca=? AND Lote=?",
                        toUpdate.Select(u => new object[] { u.Pais!, u.TipoSuelo!, u.Variedad!, u.Compania!, u.Bloque!, u.Finca!, u.Lote! })
                                .Aggregate(0, (acc, args) => acc + db.Execute(
                                    "UPDATE GeoAgrLote SET Pais=?, TipoSuelo=?, Variedad=? WHERE Compania=? AND Bloque=? AND Finca=? AND Lote=?",
                                    args[0], args[1], args[2], args[3], args[4], args[5], args[6])));
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        public async Task<bool> DeleteGeoAgrLoteAsync()
        {
            await database.ExecuteAsync("DELETE FROM GeoAgrLote");
            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // TicoFrut — GeoAgrOt
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<GeoAgrOt>> GetGeoAgrOtAsync()
        {
            var locals = await database.Table<GeoAgrOtLocal>().ToListAsync();
            return locals.Select(l => new GeoAgrOt
            {
                Compania = l.Compania,
                Bloque   = l.Bloque,
                Finca    = l.Finca,
                Ot       = l.Ot,
                NombreOt = l.NombreOt,
            }).ToList();
        }

        public async Task<List<GeoAgrOt>> GetGeoAgrOtAsync(string compania)
        {
            var locals = await database.Table<GeoAgrOtLocal>()
                .Where(l => l.Compania == compania)
                .ToListAsync();
            return locals.Select(l => new GeoAgrOt
            {
                Compania = l.Compania,
                Bloque = l.Bloque,
                Finca = l.Finca,
                Ot = l.Ot,
                NombreOt = l.NombreOt,
            }).ToList();
        }

        public async Task<List<GeoAgrOt>> GetGeoAgrOtAsync(string compania, string bloque, string finca)
        {
            var locals = await database.Table<GeoAgrOtLocal>()
                .Where(l => l.Compania == compania && l.Bloque == bloque && l.Finca == finca)
                .ToListAsync();
            return locals.Select(l => new GeoAgrOt
            {
                Compania = l.Compania,
                Bloque   = l.Bloque,
                Finca    = l.Finca,
                Ot       = l.Ot,
                NombreOt = l.NombreOt,
            }).ToList();
        }

        public async Task<GeoAgrOt?> GetGeoAgrOtAsync(string compania, string bloque, string finca, string ot)
        {
            var l = await database.Table<GeoAgrOtLocal>()
                .FirstOrDefaultAsync(i => i.Compania == compania && i.Bloque == bloque
                                       && i.Finca == finca && i.Ot == ot);
            if (l is null) return null;
            return new GeoAgrOt
            {
                Compania = l.Compania, Bloque = l.Bloque,
                Finca    = l.Finca,   Ot     = l.Ot, NombreOt = l.NombreOt,
            };
        }

        public async Task<int> SaveGeoAgrOtAsync(IEnumerable<GeoAgrOt> ots)
        {
            var list = ots.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrOtLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(l => (l.Compania!, l.Bloque!, l.Finca!, l.Ot!));

            var toInsert = new List<GeoAgrOtLocal>();
            var toUpdate = new List<GeoAgrOtLocal>();

            foreach (var item in list)
            {
                item.Bloque = item.Bloque ?? "";
                if (existingDict.TryGetValue((item.Compania!, item.Bloque!, item.Finca!, item.Ot!), out var local))
                {
                    local.NombreOt = item.NombreOt;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new GeoAgrOtLocal
                    {
                        Compania = item.Compania,
                        Bloque   = item.Bloque??"",
                        Finca    = item.Finca,
                        Ot       = item.Ot,
                        NombreOt = item.NombreOt,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                foreach (var u in toUpdate)
                    db.Execute("UPDATE GeoAgrOt SET NombreOt=? WHERE Compania=? AND Bloque=? AND Finca=? AND Ot=?",
                               u.NombreOt, u.Compania, u.Bloque, u.Finca, u.Ot);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        public async Task<bool> DeleteGeoAgrOtAsync()
        {
            await database.ExecuteAsync("DELETE FROM GeoAgrOt");
            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // TicoFrut — GeoAgrOtLabor
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<GeoAgrOtLabor>> GetGeoAgrOtLaborAsync()
        {
            var locals = await database.Table<GeoAgrOtLaborLocal>().ToListAsync();
            return locals.Select(l => new GeoAgrOtLabor
            {
                Compania    = l.Compania,
                Bloque      = l.Bloque,
                Finca       = l.Finca,
                Ot          = l.Ot,
                Labor       = l.Labor,
                NombreLabor = l.NombreLabor,
            }).ToList();
        }

        public async Task<List<GeoAgrOtLabor>> GetGeoAgrOtLaborAsync(string compania, string bloque, string finca)
        {
            var locals = await database.Table<GeoAgrOtLaborLocal>()
                .Where(l => l.Compania == compania && l.Bloque == bloque && l.Finca == finca)
                .ToListAsync();
            return locals.Select(l => new GeoAgrOtLabor
            {
                Compania    = l.Compania,
                Bloque      = l.Bloque,
                Finca       = l.Finca,
                Ot          = l.Ot,
                Labor       = l.Labor,
                NombreLabor = l.NombreLabor,
            }).ToList();
        }

        public async Task<List<GeoAgrOtLabor>> GetGeoAgrOtLaborAsync(string compania)
        {
            var locals = await database.Table<GeoAgrOtLaborLocal>()
                .Where(l => l.Compania == compania)
                .ToListAsync();
            return locals.Select(l => new GeoAgrOtLabor
            {
                Compania = l.Compania,
                Bloque = l.Bloque,
                Finca = l.Finca,
                Ot = l.Ot,
                Labor = l.Labor,
                NombreLabor = l.NombreLabor,
            }).ToList();
        }

        public async Task<List<GeoAgrOtLabor>> GetGeoAgrOtLaborAsync(string compania, string bloque, string finca, string ot)
        {
            var locals = await database.Table<GeoAgrOtLaborLocal>()
                .Where(l => l.Compania == compania && l.Bloque == bloque
                         && l.Finca == finca && l.Ot == ot)
                .ToListAsync();
            return locals.Select(l => new GeoAgrOtLabor
            {
                Compania    = l.Compania,
                Bloque      = l.Bloque,
                Finca       = l.Finca,
                Ot          = l.Ot,
                Labor       = l.Labor,
                NombreLabor = l.NombreLabor,
            }).ToList();
        }

        public async Task<GeoAgrOtLabor?> GetGeoAgrOtLaborAsync(string compania, string bloque, string finca, string ot, string labor)
        {
            var l = await database.Table<GeoAgrOtLaborLocal>()
                .FirstOrDefaultAsync(i => i.Compania == compania && i.Bloque == bloque
                                       && i.Finca == finca && i.Ot == ot && i.Labor == labor);
            if (l is null) return null;
            return new GeoAgrOtLabor
            {
                Compania    = l.Compania, Bloque = l.Bloque, Finca = l.Finca,
                Ot          = l.Ot,      Labor  = l.Labor,  NombreLabor = l.NombreLabor,
            };
        }

        public async Task<int> SaveGeoAgrOtLaborAsync(IEnumerable<GeoAgrOtLabor> labores)
        {
            var list = labores.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrOtLaborLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(l => (l.Compania!, l.Bloque!, l.Finca!, l.Ot!, l.Labor!));

            var toInsert = new List<GeoAgrOtLaborLocal>();
            var toUpdate = new List<GeoAgrOtLaborLocal>();

            foreach (var item in list)
            {
                item.Bloque = item.Bloque ?? "";
                if (existingDict.TryGetValue((item.Compania!, item.Bloque!, item.Finca!, item.Ot!, item.Labor!), out var local))
                {
                    local.NombreLabor = item.NombreLabor;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new GeoAgrOtLaborLocal
                    {
                        Compania    = item.Compania,
                        Bloque      = item.Bloque,
                        Finca       = item.Finca,
                        Ot          = item.Ot,
                        Labor       = item.Labor,
                        NombreLabor = item.NombreLabor,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                foreach (var u in toUpdate)
                    db.Execute(
                        "UPDATE GeoAgrOtLabor SET NombreLabor=? WHERE Compania=? AND Bloque=? AND Finca=? AND Ot=? AND Labor=?",
                        u.NombreLabor, u.Compania, u.Bloque, u.Finca, u.Ot, u.Labor);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        public async Task<bool> DeleteGeoAgrOtLaborAsync()
        {
            await database.ExecuteAsync("DELETE FROM GeoAgrOtLabor");
            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // TicoFrut — GeoAgrRecursoConcepto
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<GeoAgrRecursoConcepto>> GetGeoAgrRecursoConceptoAsync()
        {
            var locals = await database.Table<GeoAgrRecursoConceptoLocal>().ToListAsync();
            return locals.Select(l => new GeoAgrRecursoConcepto
            {
                Compania     = l.Compania,
                Recurso      = l.Recurso,
                Descripcion  = l.Descripcion,
                TipoFactorMo = l.TipoFactorMo,
                FactorMo     = l.FactorMo,
                ConceptoMo   = l.ConceptoMo,
                Unidad       = l.Unidad,
            }).ToList();
        }

        public async Task<List<GeoAgrRecursoConcepto>> GetGeoAgrRecursoConceptoAsync(string compania)
        {
            var locals = await database.Table<GeoAgrRecursoConceptoLocal>()
                .Where(l => l.Compania == compania)
                .ToListAsync();
            return locals.Select(l => new GeoAgrRecursoConcepto
            {
                Compania = l.Compania,
                Recurso = l.Recurso,
                Descripcion = l.Descripcion,
                TipoFactorMo = l.TipoFactorMo,
                FactorMo = l.FactorMo,
                ConceptoMo = l.ConceptoMo,
                Unidad = l.Unidad,
            }).ToList();
        }

        public async Task<GeoAgrRecursoConcepto?> GetGeoAgrRecursoConceptoAsync(string compania, string recurso)
        {
            var l = await database.Table<GeoAgrRecursoConceptoLocal>()
                .FirstOrDefaultAsync(i => i.Compania == compania && i.Recurso == recurso);
            if (l is null) return null;
            return new GeoAgrRecursoConcepto
            {
                Compania     = l.Compania,
                Recurso      = l.Recurso,
                Descripcion  = l.Descripcion,
                TipoFactorMo = l.TipoFactorMo,
                FactorMo     = l.FactorMo,
                ConceptoMo   = l.ConceptoMo,
                Unidad = l.Unidad,
            };
        }

        public async Task<int> SaveGeoAgrRecursoConceptoAsync(IEnumerable<GeoAgrRecursoConcepto> recursos)
        {
            var list = recursos.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrRecursoConceptoLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(l => (l.Compania!, l.Recurso!));

            var toInsert = new List<GeoAgrRecursoConceptoLocal>();
            var toUpdate = new List<GeoAgrRecursoConceptoLocal>();

            foreach (var item in list)
            {
                if (existingDict.TryGetValue((item.Compania!, item.Recurso!), out var local))
                {
                    local.Descripcion  = item.Descripcion;
                    local.TipoFactorMo = item.TipoFactorMo;
                    local.FactorMo     = item.FactorMo;
                    local.ConceptoMo   = item.ConceptoMo;
                    local.Unidad       = item.Unidad;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new GeoAgrRecursoConceptoLocal
                    {
                        Compania     = item.Compania,
                        Recurso      = item.Recurso,
                        Descripcion  = item.Descripcion,
                        TipoFactorMo = item.TipoFactorMo,
                        FactorMo     = item.FactorMo,
                        ConceptoMo   = item.ConceptoMo,
                        Unidad       = item.Unidad,  
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                foreach (var u in toUpdate)
                    db.Execute(
                        "UPDATE GeoAgrRecursoConcepto SET Descripcion=?, TipoFactorMo=?, FactorMo=?, ConceptoMo=?, Unidad=? WHERE Compania=? AND Recurso=?",
                        u.Descripcion, u.TipoFactorMo, u.FactorMo, u.ConceptoMo, u.Unidad, u.Compania, u.Recurso);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        public async Task<bool> DeleteGeoAgrRecursoConceptoAsync()
        {
            await database.ExecuteAsync("DELETE FROM GeoAgrRecursoConcepto");
            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // TicoFrut — GeoAgrLaborAuxiliar
        // ════════════════════════════════════════════════════════════════════════

        private static GeoAgrLaborAuxiliar MapLocalToAuxiliar(GeoAgrLaborAuxiliarLocal l) =>
            new GeoAgrLaborAuxiliar
            {
                IdRegistro  = l.IdRegistro,
                Compania    = l.Compania!,
                IdEmpleado  = l.IdEmpleado!,
                Fecha       = l.Fecha,
                Comentarios = l.Comentarios,
                EstadoId    = l.EstadoId,
            };

        public async Task<List<GeoAgrLaborAuxiliar>> GetGeoAgrLaborAuxiliarAsync()
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarLocal>().ToListAsync();
            return locals.Select(MapLocalToAuxiliar).ToList();
        }

        public async Task<List<GeoAgrLaborAuxiliar>> GetGeoAgrLaborAuxiliarAsync(string compania, string idEmpleado)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarLocal>()
                .Where(l => l.Compania == compania && l.IdEmpleado == idEmpleado)
                .ToListAsync();
            return locals.Select(MapLocalToAuxiliar).ToList();
        }

        public async Task<GeoAgrLaborAuxiliar?> GetGeoAgrLaborAuxiliarAsync(string compania, string idEmpleado, DateTime fecha, long idRegistro)
        {
            var l = await database.Table<GeoAgrLaborAuxiliarLocal>()
                .FirstOrDefaultAsync(i => i.Compania == compania
                                       && i.IdEmpleado == idEmpleado
                                       && i.Fecha == fecha
                                       && i.IdRegistro == idRegistro);
            return l is null ? null : MapLocalToAuxiliar(l);
        }

        public async Task<List<GeoAgrLaborAuxiliar>> GetGeoAgrLaborAuxiliarAsync(string compania, string idEmpleado, DateTime fechaInicio, DateTime fechaFinal)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarLocal>()
                .Where(i => i.Compania == compania
                            && i.IdEmpleado == idEmpleado
                            && i.Fecha >= fechaInicio 
                            && i.Fecha <= fechaFinal
                            && (i.EstadoId != 99 && i.EstadoId != 100))
                .ToListAsync();
            return locals.Select(MapLocalToAuxiliar).ToList();
        }

        public async Task<int> SaveGeoAgrLaborAuxiliarAsync(IEnumerable<GeoAgrLaborAuxiliar> labores)
        {
            var list = labores.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrLaborAuxiliarLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(l => (l.Compania!, l.IdEmpleado!, l.Fecha));

            var toInsert = new List<GeoAgrLaborAuxiliarLocal>();
            var toUpdate = new List<GeoAgrLaborAuxiliarLocal>();

            // Próximo IdRegistro disponible (MAX+1 o 1 si la tabla está vacía)
            long nextId = existingLocals.Count > 0
                ? existingLocals.Max(l => l.IdRegistro) + 1
                : 1;

            foreach (var item in list)
            {
                if (existingDict.TryGetValue((item.Compania, item.IdEmpleado, item.Fecha), out var local))
                {
                    local.Comentarios = item.Comentarios;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new GeoAgrLaborAuxiliarLocal
                    {
                        IdRegistro  = nextId++,
                        Compania    = item.Compania,
                        IdEmpleado  = item.IdEmpleado,
                        Fecha       = item.Fecha,
                        Comentarios = item.Comentarios,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                foreach (var u in toUpdate)
                    db.Execute(
                        @"UPDATE GeoAgrLaborAuxiliar
                          SET Comentarios=?
                          WHERE IdRegistro=? AND Compania=? AND IdEmpleado=? AND Fecha=?",
                        u.Comentarios, u.IdRegistro, u.Compania, u.IdEmpleado, u.Fecha);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        public async Task<int> SaveGeoAgrLaborAuxiliarEstadoAsync(long idRegistro, string compania)
        {
            // Marca el registro como "En Proceso de Autorización" (1), excepto los detalles ya
            // finalizados —2 (Aprobado) y 99 (Aplicado)—, que conservan su estado y no se reenvían.
            await database.ExecuteAsync(@"UPDATE GeoAgrLaborAuxiliar SET EstadoId=? WHERE IdRegistro=? AND Compania=? AND EstadoId NOT IN (2, 99)", 1, idRegistro, compania);
            await database.ExecuteAsync(@"UPDATE GeoAgrLaborAuxiliarDet SET EstadoId=? WHERE IdRegistro=? AND Compania=? AND EstadoId NOT IN (2, 99)", 1, idRegistro, compania);
            return 1;
        }

        public async Task<bool> DeleteGeoAgrLaborAuxiliarAsync()
        {
            await database.ExecuteAsync("DELETE FROM GeoAgrLaborAuxiliar");
            return true;
        }

        public async Task<bool> DeleteGeoAgrLaborAuxiliarAsync(string compania, string idEmpleado, DateTime fecha)
        {
            await database.ExecuteAsync(
                "DELETE FROM GeoAgrLaborAuxiliar WHERE Compania=? AND IdEmpleado=? AND Fecha=?",
                compania, idEmpleado, fecha);
            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // TicoFrut — GeoAgrLaborAuxiliarDet
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<GeoAgrLaborAuxiliarDet>> GetGeoAgrLaborAuxiliarDetAsync()
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetLocal>().ToListAsync();
            return locals.Select(MapDetLocalToDet).ToList();
        }

        public async Task<List<GeoAgrLaborAuxiliarDet>> GetGeoAgrLaborAuxiliarDetAsync(string compania, string idEmpleado)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetLocal>()
                .Where(l => l.Compania == compania && l.IdEmpleado == idEmpleado)
                .ToListAsync();
            return locals.Select(MapDetLocalToDet).ToList();
        }

        public async Task<List<GeoAgrLaborAuxiliarDet>> GetGeoAgrLaborAuxiliarDetAsync(string compania, string idEmpleado, DateTime fechaInicio, DateTime fechaFinal)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetLocal>()
                .Where(l => l.Compania == compania 
                        && l.IdEmpleado == idEmpleado 
                        && l.Fecha >= fechaInicio
                        && l.Fecha <= fechaFinal
                        && (l.EstadoId != 99 && l.EstadoId != 100))
                .ToListAsync();
            return locals.Select(MapDetLocalToDet).ToList();
        }

        public async Task<List<GeoAgrLaborAuxiliarDet>> GetGeoAgrLaborAuxiliarDetAsync(string compania, string idEmpleado, DateTime fecha, long IdRegistro)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetLocal>()
                .Where(l => l.Compania == compania && l.IdEmpleado == idEmpleado && l.Fecha == fecha && l.IdRegistro == IdRegistro)
                .ToListAsync();
            return locals.Select(MapDetLocalToDet).ToList();
        }

        public async Task<GeoAgrLaborAuxiliarDet?> GetGeoAgrLaborAuxiliarDetAsync(string compania, string idEmpleado, DateTime fecha, int item)
        {
            var l = await database.Table<GeoAgrLaborAuxiliarDetLocal>()
                .FirstOrDefaultAsync(i => i.Compania == compania && i.IdEmpleado == idEmpleado
                                       && i.Fecha == fecha && i.Item == item);
            if (l is null) return null;
            return MapDetLocalToDet(l);
        }

        public async Task<int> SaveGeoAgrLaborAuxiliarDetAsync(IEnumerable<GeoAgrLaborAuxiliarDet> detalles)
        {
            var list = detalles.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrLaborAuxiliarDetLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(l => (l.Compania!, l.IdEmpleado!, l.Fecha, l.Item, l.IdEmpleadoAsignado!));

            var toInsert = new List<GeoAgrLaborAuxiliarDetLocal>();
            var toUpdate = new List<GeoAgrLaborAuxiliarDetLocal>();

            // Cache de IdRegistro del encabezado padre para evitar múltiples consultas
            var idRegistroCache = new Dictionary<(string, string, DateTime), long>();

            foreach (var item in list)
            {
                var idEmpAsig = item.IdEmpleadoAsignado ?? item.IdEmpleado;

                // Resolver IdRegistro del encabezado padre
                var cacheKey = (item.Compania, item.IdEmpleado, item.Fecha);
                if (!idRegistroCache.TryGetValue(cacheKey, out long idRegistro))
                {
                    var padre = await database.Table<GeoAgrLaborAuxiliarLocal>()
                        .FirstOrDefaultAsync(l => l.Compania   == item.Compania
                                               && l.IdEmpleado == item.IdEmpleado
                                               && l.Fecha      == item.Fecha);
                    idRegistro = padre?.IdRegistro ?? 0;
                    idRegistroCache[cacheKey] = idRegistro;
                }

                if (existingDict.TryGetValue((item.Compania, item.IdEmpleado, item.Fecha, item.Item, idEmpAsig), out var local))
                {
                    local.IdRegistro          = idRegistro;
                    local.Bloque              = item.Bloque;
                    local.Finca               = item.Finca;
                    local.Lote                = item.Lote;
                    local.Ot                  = item.Ot;
                    local.Labor               = item.Labor;
                    local.CantidadRendimiento = item.CantidadRendimiento;
                    local.Unidad              = item.Unidad;
                    local.HorasAprobadas      = item.HorasAprobadas;
                    local.EstadoId            = item.EstadoId;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new GeoAgrLaborAuxiliarDetLocal
                    {
                        IdRegistro           = idRegistro,
                        Compania             = item.Compania,
                        IdEmpleado           = item.IdEmpleado,
                        Fecha                = item.Fecha,
                        Item                 = item.Item,
                        Bloque               = item.Bloque,
                        Finca                = item.Finca,
                        Lote                 = item.Lote,
                        Ot                   = item.Ot,
                        Labor                = item.Labor,
                        CantidadRendimiento  = item.CantidadRendimiento,
                        Unidad               = item.Unidad,
                        HorasAprobadas       = item.HorasAprobadas,
                        EstadoId             = item.EstadoId,
                        IdEmpleadoAsignado   = idEmpAsig,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                foreach (var u in toUpdate)
                    db.Execute(
                        @"UPDATE GeoAgrLaborAuxiliarDet
                          SET IdRegistro=?, Bloque=?, Finca=?, Lote=?, Ot=?, Labor=?,
                              CantidadRendimiento=?, Unidad=?, HorasAprobadas=?, EstadoId=?
                          WHERE Compania=? AND IdEmpleado=? AND Fecha=? AND Item=? AND IdEmpleadoAsignado=?",
                        u.IdRegistro, u.Bloque, u.Finca, u.Lote, u.Ot, u.Labor,
                        u.CantidadRendimiento, u.Unidad, u.HorasAprobadas, u.EstadoId,
                        u.Compania, u.IdEmpleado, u.Fecha, u.Item, u.IdEmpleadoAsignado);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        public async Task<bool> DeleteGeoAgrLaborAuxiliarDetAsync()
        {
            await database.ExecuteAsync("DELETE FROM GeoAgrLaborAuxiliarDet");
            return true;
        }

        public async Task<bool> DeleteGeoAgrLaborAuxiliarDetAsync(string compania, string idEmpleado, DateTime fecha)
        {
            await database.ExecuteAsync(
                "DELETE FROM GeoAgrLaborAuxiliarDet WHERE Compania=? AND IdEmpleado=? AND Fecha=?",
                compania, idEmpleado, fecha);
            return true;
        }

        private static GeoAgrLaborAuxiliarDet MapDetLocalToDet(GeoAgrLaborAuxiliarDetLocal l) =>
            new GeoAgrLaborAuxiliarDet
            {
                Compania            = l.Compania!,
                IdEmpleado          = l.IdEmpleado!,
                Fecha               = l.Fecha,
                Item                = l.Item,
                Bloque              = l.Bloque,
                Finca               = l.Finca,
                Lote                = l.Lote,
                Ot                  = l.Ot,
                Labor               = l.Labor,
                CantidadRendimiento = l.CantidadRendimiento,
                Unidad              = l.Unidad,
                Recurso             = l.Recurso,
                HorasAprobadas      = l.HorasAprobadas,
                EstadoId            = l.EstadoId,
                IdRegistro          = l.IdRegistro,
                IdEmpleadoAsignado  = l.IdEmpleadoAsignado,
            };

        // ════════════════════════════════════════════════════════════════════════
        // FiltroUsuario — último País y Compañía seleccionados por empleado
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Devuelve el filtro guardado del usuario, o null si nunca ha seleccionado.
        /// </summary>
        public async Task<FiltroUsuarioLocal?> GetFiltroUsuarioAsync(string idUsuario)
        {
            return await database.Table<FiltroUsuarioLocal>()
                                 .FirstOrDefaultAsync(f => f.IdUsuario == idUsuario);
        }

        /// <summary>
        /// Inserta o actualiza el filtro del usuario (upsert por IdUsuario).
        /// </summary>
        public async Task SaveFiltroUsuarioAsync(string idUsuario, string pais, string compania)
        {
            var existing = await GetFiltroUsuarioAsync(idUsuario);
            if (existing is null)
            {
                await database.InsertAsync(new FiltroUsuarioLocal
                {
                    IdUsuario = idUsuario,
                    Pais      = pais,
                    Compania  = compania
                });
            }
            else
            {
                existing.Pais     = pais;
                existing.Compania = compania;
                await database.UpdateAsync(existing);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // TicoFrut — GeoAgrLaborAuxiliar  (métodos por IdRegistro)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Devuelve la lista raw (con IdRegistro) de todas las labores auxiliares.</summary>
        public async Task<List<GeoAgrLaborAuxiliarLocal>> GetGeoAgrLaborAuxiliarLocalesAsync()
        {
            return await database.Table<GeoAgrLaborAuxiliarLocal>().ToListAsync();
        }

        /// <summary>
        /// Siempre inserta un nuevo encabezado (nunca upsert) y devuelve el IdRegistro asignado.
        /// </summary>
        public async Task<long> InsertGeoAgrLaborAuxiliarAsync(GeoAgrLaborAuxiliar labor)
        {
            var existingLocals = await database.Table<GeoAgrLaborAuxiliarLocal>().ToListAsync();
            long nextId = existingLocals.Count > 0
                ? existingLocals.Max(l => l.IdRegistro) + 1
                : 1;

            var local = new GeoAgrLaborAuxiliarLocal
            {
                IdRegistro  = nextId,
                Compania    = labor.Compania,
                IdEmpleado  = labor.IdEmpleado,
                Fecha       = labor.Fecha,
                Comentarios = labor.Comentarios,
            };
            await database.InsertAsync(local);
            return nextId;
        }

        /// <summary>Actualiza solo los campos editables del encabezado, identificado por IdRegistro.</summary>
        public async Task UpdateGeoAgrLaborAuxiliarAsync(long idRegistro, GeoAgrLaborAuxiliar labor)
        {
            await database.ExecuteAsync(
                "UPDATE GeoAgrLaborAuxiliar SET Comentarios=? WHERE IdRegistro=?",
                labor.Comentarios, idRegistro);
        }

        /// <summary>Elimina el encabezado por IdRegistro (los hijos se borran por CASCADE).</summary>
        public async Task<bool> DeleteGeoAgrLaborAuxiliarAsync(long idRegistro)
        {
            await database.ExecuteAsync(
                "DELETE FROM GeoAgrLaborAuxiliar WHERE IdRegistro=?",
                idRegistro);
            return true;
        }

        // ── Det por IdRegistro ───────────────────────────────────────────────────

        /// <summary>Elimina todos los detalles de una labor identificada por IdRegistro.</summary>
        public async Task<bool> DeleteGeoAgrLaborAuxiliarDetAsync(long idRegistro)
        {
            await database.ExecuteAsync(
                "DELETE FROM GeoAgrLaborAuxiliarDet WHERE IdRegistro=?",
                idRegistro);
            return true;
        }

        /// <summary>
        /// Inserta (sin upsert) todos los detalles asociando el IdRegistro dado.
        /// Se llama tras borrar los detalles anteriores con DeleteGeoAgrLaborAuxiliarDetAsync(idRegistro).
        /// </summary>
        public async Task<int> SaveGeoAgrLaborAuxiliarDetAsync(
            long idRegistro, IEnumerable<GeoAgrLaborAuxiliarDet> detalles)
        {
            var list = detalles.ToList();
            if (list.Count == 0) return 0;

            var toInsert = list.Select(item => new GeoAgrLaborAuxiliarDetLocal
            {
                IdRegistro          = idRegistro,
                Compania            = item.Compania,
                IdEmpleado          = item.IdEmpleado,
                Fecha               = item.Fecha,
                Item                = item.Item,
                Bloque              = item.Bloque,
                Finca               = item.Finca,
                Lote                = item.Lote,
                Ot                  = item.Ot,
                Labor               = item.Labor,
                CantidadRendimiento = item.CantidadRendimiento,
                Recurso             = item.Recurso,
                Unidad              = item.Unidad,
                HorasAprobadas      = item.HorasAprobadas,
                IdEmpleadoAsignado  = item.IdEmpleadoAsignado,
            }).ToList();

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                totalAffected = db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        // ── Actualización de cantidad asignada por empleado ──────────────────────

        /// <summary>Actualiza la cantidad asignada (CantidadRendimiento) de cada empleado para un ítem específico. Solo local — no reenvía a Geotime.</summary>
        public async Task UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync(
            long idRegistro, int item, IEnumerable<(string IdEmpleadoAsignado, decimal? CantidadAsignada)> cantidades)
        {
            await database.RunInTransactionAsync(db =>
            {
                foreach (var (idEmp, cant) in cantidades)
                    db.Execute(
                        "UPDATE GeoAgrLaborAuxiliarDet SET CantidadRendimiento=? WHERE IdRegistro=? AND Item=? AND IdEmpleadoAsignado=?",
                        cant, idRegistro, item, idEmp);
            });
        }

        // ════════════════════════════════════════════════════════════════════════
        // cEstado — catálogo de estados (sincronizado desde Geotime)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Retorna todos los estados almacenados localmente.</summary>
        public async Task<List<cEstado>> GetEstadoAsync()
        {
            var l = await database.Table<EstadoLocal>().ToListAsync();

            if (l is null) return null;
            return l.Select(item => new cEstado
            {
                Id = item.Id,
                Descripcion = item.Descripcion!,
                FechaRegistro = DateTime.Now
            }).ToList();
        }

        public async Task<cEstado> GetEstadoAsync(int id)
        {
            var l = await database.Table<EstadoLocal>()
                            .FirstOrDefaultAsync(i => i.Id == id);
            if (l is null) return null;
            return new cEstado
            {
                Id = l.Id,
                Descripcion = l.Descripcion!,
                FechaRegistro = DateTime.Now
            };
        }

        /// <summary>
        /// Guarda (upsert) la lista de estados proveniente de Geotime.
        /// Inserta los nuevos y actualiza Descripcion/FechaRegistro de los existentes.
        /// </summary>
        public async Task<int> SaveEstadoAsync(IEnumerable<cEstado> estados)
        {
            var list = estados?.ToList();
            if (list is null || list.Count == 0) return 0;

            var existingLocals = await database.Table<EstadoLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(e => e.Id);

            var toInsert = new List<EstadoLocal>();
            var toUpdate = new List<EstadoLocal>();
            foreach (var item in list)
            {
                if (existingDict.TryGetValue(item.Id, out var local))
                {
                    local.Descripcion    = item.Descripcion;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new EstadoLocal
                    {
                        Id            = item.Id,
                        Descripcion   = item.Descripcion,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                foreach (var u in toUpdate)
                    totalAffected += db.Execute(
                        "UPDATE cEstado SET Descripcion=? WHERE Id=?",
                        u.Descripcion, u.Id);

                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        public cPh_Login MapPhLoginLocalToPhLogin(cPhLoginLocal l)
        {
            return new cPh_Login
            {
                idusuario = l.idusuario,
                usuario = l.usuario,
                clave = l.clave,
                descripcion = l.descripcion,
                privilegio = l.privilegio,
                grupo_base = l.grupo_base,
                idsesion = l.idsesion,
                ultimo_login = l.ultimo_login,
                fcomp = l.fcomp,
                fplani = l.fplani,
                fperiodo = l.fperiodo,
                grupos = l.grupos,
                proceso = l.proceso,
                ultimo_estado = l.ultimo_estado,
                companias = l.companias,
                planillas = l.planillas,
                idioma = l.idioma,
                stat_msg = l.stat_msg,
                turnos = l.turnos,
                usa_wusuario = string.IsNullOrEmpty(l.usa_wusuario) ? null : l.usa_wusuario[0],
                estado = string.IsNullOrEmpty(l.estado) ? '\0' : l.estado[0],
                EMAIL = l.EMAIL,
                OMITE_LIC = string.IsNullOrEmpty(l.OMITE_LIC) ? null : l.OMITE_LIC[0],
                ACTIVO = string.IsNullOrEmpty(l.ACTIVO) ? null : l.ACTIVO[0],
                GLOBAL_CLAVE = l.GLOBAL_CLAVE,
                ROLUSUARIO = l.ROLUSUARIO
            };
        }

        public async Task<List<cPh_Login>> GetPhLoginAsync()
        {
            var locals = await database.Table<cPhLoginLocal>().ToListAsync();
            return locals.Select(l => MapPhLoginLocalToPhLogin(l)).ToList();
        }

        public async Task<cPh_Login> GetPhLoginAsync(string id)
        {
            var l = await database.Table<cPhLoginLocal>()
                            .FirstOrDefaultAsync(i => i.usuario == id);
            if (l is null) return null;
            return MapPhLoginLocalToPhLogin(l);
        }

        public async Task<int> SavePhLoginAsync(IEnumerable<cPh_Login> logins)
        {
            var loginsList = logins.ToList();
            if (loginsList.Count == 0) return 0;

            var existingLocals = await database.Table<cPhLoginLocal>().ToListAsync();
            var existingDict = existingLocals.ToDictionary(l => l.idusuario);

            var toInsert = new List<cPhLoginLocal>();
            var toUpdate = new List<cPhLoginLocal>();

            foreach (var login in loginsList)
            {
                if (existingDict.TryGetValue(login.idusuario, out var local))
                {
                    local.usuario = login.usuario;
                    local.clave = login.clave;
                    local.descripcion = login.descripcion;
                    local.privilegio = login.privilegio;
                    local.grupo_base = login.grupo_base;
                    local.idsesion = login.idsesion;
                    local.ultimo_login = login.ultimo_login;
                    local.fcomp = login.fcomp;
                    local.fplani = login.fplani;
                    local.fperiodo = login.fperiodo;
                    local.grupos = login.grupos;
                    local.proceso = login.proceso;
                    local.ultimo_estado = login.ultimo_estado;
                    local.companias = login.companias;
                    local.planillas = login.planillas;
                    local.idioma = login.idioma;
                    local.stat_msg = login.stat_msg;
                    local.turnos = login.turnos;
                    local.usa_wusuario = login.usa_wusuario == null ? null : login.usa_wusuario.ToString();
                    local.estado = login.estado.ToString();
                    local.EMAIL = login.EMAIL;
                    local.OMITE_LIC = login.OMITE_LIC == null ? null : login.OMITE_LIC.ToString();
                    local.ACTIVO = login.ACTIVO == null ? null : login.ACTIVO.ToString();
                    local.GLOBAL_CLAVE = login.GLOBAL_CLAVE;
                    local.ROLUSUARIO = login.ROLUSUARIO;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new cPhLoginLocal
                    {
                        idusuario = login.idusuario,
                        usuario = login.usuario,
                        clave = login.clave,
                        descripcion = login.descripcion,
                        privilegio = login.privilegio,
                        grupo_base = login.grupo_base,
                        idsesion = login.idsesion,
                        ultimo_login = login.ultimo_login,
                        fcomp = login.fcomp,
                        fplani = login.fplani,
                        fperiodo = login.fperiodo,
                        grupos = login.grupos,
                        proceso = login.proceso,
                        ultimo_estado = login.ultimo_estado,
                        companias = login.companias,
                        planillas = login.planillas,
                        idioma = login.idioma,
                        stat_msg = login.stat_msg,
                        turnos = login.turnos,
                        usa_wusuario = login.usa_wusuario == null ? null : login.usa_wusuario.ToString(),
                        estado = login.estado.ToString(),
                        EMAIL = login.EMAIL,
                        OMITE_LIC = login.OMITE_LIC == null ? null : login.OMITE_LIC.ToString(),
                        ACTIVO = login.ACTIVO == null ? null : login.ACTIVO.ToString(),
                        GLOBAL_CLAVE = login.GLOBAL_CLAVE,
                        ROLUSUARIO = login.ROLUSUARIO
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                if (toUpdate.Count > 0)
                    totalAffected += db.UpdateAll(toUpdate);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });

            return totalAffected;
        }

    }
}
