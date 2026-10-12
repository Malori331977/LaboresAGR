# Catálogos de labores desde la API de Geotime y base local LaboresAGR

Fecha: 2026-10-12 · Proyecto: `LaboresNet` (MAUI Blazor) · Estado: pendiente de revisión

## 1. Objetivo

En esta versión la app **deja de usar la API de Ticofrut**. Los catálogos de labores
(bloques, fincas, lotes, órdenes de trabajo, actividades, unidades y recursos) se consultan
a la API de Geotime (`GeotimeConnectApiV2`), que los lee de las tablas `Ph_Labores*` de la
base de datos de Geotime. La base SQLite local pasa a llamarse **LaboresAGR** para no chocar
con otra base local ya existente.

Decisiones tomadas con el usuario:

- `Compania` y `Pais` se eliminan de las llaves de los catálogos y de todos los filtros de negocio.
- La compañía elegida en el login se **mantiene** únicamente como selector de tenant de la API
  de Geotime (`GeoTimeConnect.setSchema`); no es un dato de negocio.
- Cuando la API de Geotime exija `COMPANIA` al guardar una solicitud de labor
  (`cSolicitudAuxLabor`, `cSolicitudAuxLaborDet`), se toma de la compañía del tenant de la sesión.
- El semáforo `AV` (Ticofrut) se elimina. Se conserva el indicador de Geotime.
- Los catálogos locales se recrean desde cero; solo se migran las labores pendientes.

## 2. Fuente de datos: API de Geotime

Controladores en `GeotimeConnectApiV2\GeotimeConnectWebApi\Controllers\Labores`, protegidos con
`[Authorize]`. Modelos en `GeotimeModelsLib` (ya incluidos en `Dll\GeotimeModelsLib.dll`).

| Endpoint (GET) | Modelo | Tabla | Llave | Filtros opcionales |
|---|---|---|---|---|
| `/PhLaboresBloque` | `cPh_LaboresBloque` | `Ph_LaboresBloques` | `Id` | – |
| `/PhLaboresFinca` | `cPh_LaboresFinca` | `Ph_LaboresFincas` | `BloqueId, Id` | `bloqueId` |
| `/PhLaboresLote` | `cPh_LaboresLote` | `Ph_LaboresLotes` | `BloqueId, FincaId, Id` | `bloqueId`, `fincaId` |
| `/PhLaboresOrdenTrabajo` | `cPh_LaboresOrdenTrabajo` | `Ph_LaboresOrdenesTrabajo` | `BloqueId, FincaId, Id` | `bloqueId`, `fincaId` |
| `/PhLaboresActividad` | `cPh_LaboresActividad` | `Ph_LaboresActividades` | `BloqueId, FincaId, OrdenTrabajoId, Id` | `bloqueId`, `fincaId`, `ordenTrabajoId` |
| `/PhLaboresUnidad` | `cPh_LaboresUnidad` | `Ph_LaboresUnidades` | `Id` | – |
| `/PhLaboresRecurso` | `cPh_LaboresRecurso` | `Ph_LaboresRecursos` | `Id` | `unidadId`, `idConcepto` |

Campos comunes: `Descripcion`, `Estado` (bool) y auditoría (`UsuarioCreacion`, `UsuarioModificacion`,
`FechaCreacion`, `FechaModificacion`). `cPh_LaboresRecurso` agrega `IdConcepto` (int) y `UnidadId`.
Las propiedades de navegación (`cPh_Labores*`) no se persisten localmente.

La ruta exacta de cada controlador se verifica contra el atributo `[Route]` al implementar
(el controlador de OT usa `[Route("[controller]")]` sobre `PhLaboresOrdenTrabajoController`).

## 3. Equivalencias con el modelo anterior

| Ticofrut (`TicoFrutLibs`) | Geotime (nuevo) | Notas |
|---|---|---|
| `GeoAgrFinca` (Compania, Pais, Bloque, Finca) | `cPh_LaboresFinca` | Se pierde Compania/Pais; `Bloque` pasa a catálogo propio |
| `GeoAgrLote` (… TipoSuelo, Variedad) | `cPh_LaboresLote` | `TipoSuelo` y `Variedad` ya no existen |
| `GeoAgrOt` (Ot, NombreOt) | `cPh_LaboresOrdenTrabajo` (Id, Descripcion) | |
| `GeoAgrOtLabor` (Labor, NombreLabor) | `cPh_LaboresActividad` (Id, Descripcion) | Cuelga de la OT (`OrdenTrabajoId`) |
| `GeoAgrRecursoConcepto` (TipoFactorMo, FactorMo, ConceptoMo, Unidad) | `cPh_LaboresRecurso` (IdConcepto, UnidadId) | Se pierden factores de mano de obra; `Unidad` es un catálogo |
| (no existía) | `cPh_LaboresBloque`, `cPh_LaboresUnidad` | Catálogos nuevos |

Las columnas de texto del detalle de labor (`BLOQUE`, `FINCA`, `LOTE`, `OT`, `LABOR`, `RECURSO`,
`UNIDAD` en `cSolicitudAuxLaborDet`) siguen guardando los códigos; ahora son los `Id` de los
nuevos catálogos (`LABOR` = `Actividad.Id`, `UNIDAD` = `Unidad.Id`).

## 4. Diseño

### 4.1 Capa de acceso a la API

`Data\Interfaces\IGeoTimeConnect.cs` y `Data\GeoTimeConnect.cs` reciben siete métodos de lectura,
con el mismo patrón que los existentes (`Get(method)` + `ReadFromJsonAsync`, y lista vacía ante error):

- `GetLaboresBloques()`
- `GetLaboresFincas(string? bloqueId = null)`
- `GetLaboresLotes(string? bloqueId = null, string? fincaId = null)`
- `GetLaboresOrdenesTrabajo(string? bloqueId = null, string? fincaId = null)`
- `GetLaboresActividades(string? bloqueId = null, string? fincaId = null, string? ordenTrabajoId = null)`
- `GetLaboresUnidades()`
- `GetLaboresRecursos(string? unidadId = null, int? idConcepto = null)`

La sincronización llama a los siete sin filtros. Solo se escriben localmente los registros con
`Estado == true`.

### 4.2 Base local LaboresAGR

`Data\DataBaseLocal\MarcasDBLocal.cs` deja de abrir `AvancesMoDB\LaboresDatabase.db3`:

- Carpeta: `LocalApplicationData\LaboresAGR`
- Archivo: `LaboresAGR.db3`

(Supuesto: carpeta y archivo llevan el mismo nombre. Confirmar al revisar el spec.)

Tablas de catálogo nuevas, creadas con `CREATE TABLE IF NOT EXISTS`, sin `Compania` ni `Pais`:

| Tabla | Columnas | PK |
|---|---|---|
| `LaboresBloque` | Id, Descripcion | Id |
| `LaboresFinca` | BloqueId, Id, Descripcion | BloqueId, Id |
| `LaboresLote` | BloqueId, FincaId, Id, Descripcion | BloqueId, FincaId, Id |
| `LaboresOrdenTrabajo` | BloqueId, FincaId, Id, Descripcion | BloqueId, FincaId, Id |
| `LaboresActividad` | BloqueId, FincaId, OrdenTrabajoId, Id, Descripcion | BloqueId, FincaId, OrdenTrabajoId, Id |
| `LaboresUnidad` | Id, Descripcion | Id |
| `LaboresRecurso` | Id, Descripcion, IdConcepto, UnidadId | Id |

Tablas de labor auxiliar (`GeoAgrLaborAuxiliar`, `GeoAgrLaborAuxiliarDet`): se conservan nombre y
columnas, **sin `Compania`** en columnas ni en la PK. `GeoAgrLaborAuxiliar` queda con PK
`(IdRegistro, IdEmpleado, Fecha)`.

Las tablas `GeoAgrFinca`, `GeoAgrLote`, `GeoAgrOt`, `GeoAgrOtLabor` y `GeoAgrRecursoConcepto` no se crean.
`FiltroUsuario` pierde `Pais` y `Compania`.

Las tablas ajenas a labores que ya crea `MarcasDBLocal` (login, empleados, periodos, compañías del
login, etc.) no se tocan; `cPh_CompaniaLocal` sigue alimentando el selector de tenant del login.

### 4.3 Migración de labores pendientes

Al crear LaboresAGR por primera vez, un paso único copia las labores pendientes desde la base
anterior, si existe:

1. Abrir `AvancesMoDB\LaboresDatabase.db3` en solo lectura y comprobar qué tablas existen
   (la base anterior puede ser de otra aplicación; cada paso es tolerante a tablas ausentes).
2. Copiar la configuración de la API (`cParametroApiLocal`, Id 1). Esta configuración vive en la
   base local, de modo que sin este paso la app pediría configurar las APIs de nuevo.
   Los campos `ApiAuxiliarUrl` y `ApiAuxiliarSchema` (Ticofrut) no se copian.
3. Copiar las labores auxiliares **no** finalizadas (`EstadoId` distinto de 2 y 99, que son los
   estados finalizados según el historial del proyecto) y sus detalles, omitiendo la columna `Compania`.
4. Registrar la migración en una tabla de control (`MigracionLabores`) para no repetirla.

La base anterior no se modifica ni se borra. Si algo falla, se registra el error y la app arranca
con las tablas vacías. Dado que LaboresAGR es una base nueva, el usuario debe iniciar sesión en
línea una vez para repoblar los datos de login.

### 4.4 Sincronización

`Components\Pages\Principal.razor` y `Components\Pages\Procesos\SincronizacionManual.razor`:

- Se elimina el bloque "Sincronización TicoFrut" y la llamada a `ticoFrutConnect.ApiDisponible()`.
- Se llaman en paralelo los siete `GetLabores*` y se guardan con nuevos `SaveLabores*Async`
  (reemplazo completo por tabla: borrar y volver a insertar en una transacción).
- El mensaje de falta de comunicación se refiere a Geotime.

### 4.5 Interfaz

- `GeoAgrFincaPop`, `GeoAgrLotePop`, `GeoAgrOtPop`, `GeoAgrOtLaborPop` y `GeoAgrRecursoConceptoPop`
  leen los nuevos modelos locales. Se conservan los nombres de los componentes para no romper
  referencias; se renombra solo lo que muestre texto de usuario ("Labor" pasa a "Actividad" si la UI lo dice).
- `GeoAgrLaborAuxiliarEdicion`, `GeoAgrLaborAuxiliarDetalle` y `GeoAgrLaborAuxiliarList` dejan de
  filtrar y guardar `Compania` como dato propio.
- `MainLayout.razor`: se eliminan los selectores País/Compañía y su carga desde fincas;
  `LoginState` pierde `PaisSeleccionado` y `CompaniaSeleccionada` de negocio (la compañía del
  login queda en `login.Compania` y en `setSchema`).
- `ApiSemaforo.razor`: se elimina el indicador `AV` y el parámetro `TicoFrutOk`; se conserva `GT`.
  `MainLayout.razor` y `Home.razor` dejan de calcular `ticoFrutOk`.

### 4.6 Envío a Geotime

`cSolicitudAuxLabor.COMPANIA` y `cSolicitudAuxLaborDet.COMPANIA` se rellenan con la compañía del
tenant de la sesión al armar la solicitud. Las llamadas
`GetSolicitudAuxLaborByEstado/ByResponsable` siguen recibiendo la compañía de la sesión.
Esto evita cambiar contratos de la API.

### 4.7 Limpieza de Ticofrut

- Eliminar `Data\TicoFrutConnect.cs` y `Data\Interfaces\ITicoFrutConnect.cs`.
- Quitar `AddScoped<ITicoFrutConnect, TicoFrutConnect>()` de `MauiProgram.cs`.
- Quitar `@using TicoFrutLibs.Models` y `@inject ITicoFrutConnect ticoFrutConnect` de `_Imports.razor`.
- Quitar `using TicoFrutLibs.Models` de `MarcasDBLocal.cs`, tras reemplazar sus tipos
  (`GeoAgrFinca`, `GeoAgrLote`, `GeoAgrOt`, `GeoAgrOtLabor`, `GeoAgrRecursoConcepto`, `GeoAgrLaborAuxiliar`).
- Quitar `ApiAuxiliarUrl` y `ApiAuxiliarSchema` de `cParametroApiLocal`, de `ConfigFile.cs` y de
  `ConfiguracionApis.razor` (eran la configuración de la API de Ticofrut).
- Quitar la referencia a `Dll\TicoFrutLibs.dll` del `.csproj` al final, cuando no quede ningún uso.
  `Modelo\GeoAgrLaborAuxiliar.cs` y `Modelo\GeoAgrLaborAuxiliarDet.cs` son modelos propios y se quedan
  (sin `Compania`).
- `ConfiguracionApis.razor`: revisar y quitar lo que configure la URL de Ticofrut.

## 5. Manejo de errores

- Si la API de Geotime no responde durante la sincronización, no se toca el catálogo local
  (se evita dejarlo vacío); se avisa al usuario con el mismo `swal` que ya usa la app.
- La sustitución de una tabla de catálogo se hace en una transacción: o queda completa o no cambia.
- Un catálogo vacío devuelto por la API con éxito sí reemplaza al local.

## 6. Verificación

1. `dotnet build` de `LaboresNet` sin referencias a `TicoFrutLibs` ni `ITicoFrutConnect`.
2. Búsqueda en el código: sin ocurrencias de `Compania`/`Pais` fuera del login, de `setSchema` y de
   los campos `COMPANIA` de `cSolicitudAuxLabor*`.
3. Contraste de las siete tablas contra la base real de Geotime cuando el MCP `sqlserver` conecte
   (columnas, tamaños, filas con `Estado = 0`).
4. Prueba manual: primer arranque (se crea `LaboresAGR.db3`), migración de una labor pendiente,
   sincronización, captura de una labor con bloque → finca → lote → OT → actividad → recurso, envío a Geotime.

## 7. Fuera de alcance

- Edición de catálogos desde la app (los `Post/Put/Delete` de la API existen pero no se usan).
- Permisos de labores (`cPh_LaboresUsuario`, `cPh_LaboresOpcion`).
- Funciones de marcación y demás tablas ajenas a labores.
