# Catálogos de labores desde Geotime y base local LaboresAGR — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar la API de Ticofrut por los métodos de la API de Geotime (tablas `Ph_Labores*`) para todos los catálogos de labores, eliminar `Compania`/`Pais` de las llaves y filtros de negocio, y mover la base SQLite local a `LaboresAGR`.

**Architecture:** `IGeoTimeConnect` gana 7 métodos de lectura (`GetLabores*`) que devuelven `null` ante fallo. Un helper `LaboresCatalogoSync` descarga los 7 catálogos y, solo si todos respondieron, los reemplaza en SQLite dentro de transacciones. Las pantallas usan directamente los modelos `cPh_Labores*` de `GeotimeModelsLib.dll`, sin DTOs intermedios. La compañía del login queda únicamente como tenant (`LoginState.CompaniaTenant`) y se usa solo para `setSchema` y para el campo `COMPANIA` de las solicitudes enviadas a Geotime.

**Tech Stack:** .NET 10 MAUI Blazor Hybrid (`net10.0-windows10.0.19041.0`), sqlite-net-pcl, Radzen.Blazor, `GeotimeModelsLib.dll`.

**Spec:** `docs/superpowers/specs/2026-10-12-catalogos-geotime-laboresagr-design.md`

## Global Constraints

- Base local: carpeta `LocalApplicationData\LaboresAGR`, archivo `LaboresAGR.db3`.
- Base anterior (solo lectura, nunca se modifica ni se borra): `LocalApplicationData\AvancesMoDB\LaboresDatabase.db3`.
- Tablas de catálogo locales: `LaboresBloque`, `LaboresFinca`, `LaboresLote`, `LaboresOrdenTrabajo`, `LaboresActividad`, `LaboresUnidad`, `LaboresRecurso`, sin `Compania` ni `Pais`.
- Estados finalizados de una labor: `2` (Aprobado) y `99` (Aplicado); no se reenvían ni se migran.
- Solo se persisten localmente registros con `Estado == true`.
- Si la API de Geotime falla al sincronizar catálogos, el catálogo local no se toca.
- La compañía del login se conserva solo como tenant (`setSchema`) y como `COMPANIA` en `cSolicitudAuxLabor` / `cSolicitudAuxLaborDet`.
- Los componentes `GeoAgr*Pop.razor` y las páginas `GeoAgrLaborAuxiliar*.razor` conservan su nombre y ruta.
- Texto de usuario en español; las etiquetas "Labor", "OT" y "Recurso" de la UI no cambian.
- Comando de compilación (no hay proyecto de pruebas en la solución):
  `dotnet build D:\Source\Geotime\LaboresNetMaui\LaboresNet\LaboresNet.csproj -f net10.0-windows10.0.19041.0`

## Review Focus

Entradas y condiciones que el spec implica pero que ninguna prueba automática cubre (no hay proyecto de pruebas); cada una se verifica en el paso indicado:

1. **Fallo de API vs. catálogo vacío:** una respuesta HTTP no exitosa o una excepción no debe vaciar el catálogo local; una lista vacía con éxito sí lo reemplaza (Task 2, Task 3).
2. **Solicitudes antiguas con otra `COMPANIA`:** las enviadas antes del cambio usan el código de compañía de Ticofrut; `GetSolicitudAuxLaborByResponsable` con la compañía del tenant no las devolverá. Se acepta y se documenta en el resultado final (Task 4).
3. **Catálogos vacíos en el primer arranque sin conexión:** los pops deben mostrar un aviso, no una lista vacía muda ni una excepción (Task 3).
4. **Códigos huérfanos:** una línea guardada que referencia una OT, actividad o recurso con `Estado = false` o ya inexistente debe mostrar nombre vacío y no fallar (Task 3).
5. **Misma `FincaId` en bloques distintos:** todas las búsquedas usan la llave compuesta completa (`BloqueId`+`FincaId`+…), nunca solo `Id` (Task 3).
6. **Migración:** base anterior ausente, bloqueada, sin tablas esperadas o con columnas distintas; no debe impedir el arranque ni repetirse (Task 5).
7. **Tenant vacío:** si `CompaniaTenant` está vacío al enviar una labor, no se envía con `COMPANIA = ""`; se avisa (Task 4).

---

## Task 0: Línea base de git (requiere confirmación del usuario)

El repositorio no tiene ningún commit y todo el árbol está sin seguimiento. Sin una línea base, los commits por tarea no mostrarían qué cambió.

**Files:** (ninguno modificado)

- [ ] **Step 1: Preguntar al usuario**

Preguntar: "¿Quieres que cree un commit inicial con el estado actual del proyecto para poder revisar cada tarea como diff?". Si la respuesta es no, omitir todos los pasos `git commit` de este plan y dejar los cambios sin commitear.

- [ ] **Step 2: Si aceptó, crear el commit base**

```bash
cd D:/Source/Geotime/LaboresNetMaui
git status --short
git add -A
git commit -m "chore: línea base antes de migrar catálogos a Geotime"
```

Expected: `git log --oneline` muestra 1 commit. Verificar antes con `git status --short` que `bin/`, `obj/` y `Dll\*.pdb` estén cubiertos por `.gitignore`; si aparecen, añadirlos a `.gitignore` antes de `git add -A`.

---

## Task 1: Lectura de catálogos en `IGeoTimeConnect` / `GeoTimeConnect`

**Files:**
- Modify: `LaboresNet/Data/Interfaces/IGeoTimeConnect.cs` (agregar al final de la interfaz, antes de la `}` que cierra `IGeoTimeConnect`)
- Modify: `LaboresNet/Data/GeoTimeConnect.cs` (agregar después de `GetCentroCosto`, ~línea 1408)

**Interfaces:**
- Consumes: `GeoTimeConnect.Get(string method) : Task<HttpResponseMessage>` (ya existe, línea 394).
- Produces (los usa Task 3):
  ```csharp
  Task<IEnumerable<cPh_LaboresBloque>?>       GetLaboresBloques();
  Task<IEnumerable<cPh_LaboresFinca>?>        GetLaboresFincas(string? bloqueId = null);
  Task<IEnumerable<cPh_LaboresLote>?>         GetLaboresLotes(string? bloqueId = null, string? fincaId = null);
  Task<IEnumerable<cPh_LaboresOrdenTrabajo>?> GetLaboresOrdenesTrabajo(string? bloqueId = null, string? fincaId = null);
  Task<IEnumerable<cPh_LaboresActividad>?>    GetLaboresActividades(string? bloqueId = null, string? fincaId = null, string? ordenTrabajoId = null);
  Task<IEnumerable<cPh_LaboresUnidad>?>       GetLaboresUnidades();
  Task<IEnumerable<cPh_LaboresRecurso>?>      GetLaboresRecursos(string? unidadId = null, int? idConcepto = null);
  ```
  Devuelven `null` si la API respondió con error o lanzó excepción; lista (posiblemente vacía) si respondió con éxito.

- [ ] **Step 1: Confirmar las rutas reales de los controladores**

Run: `Grep` del patrón `Route\(` en `D:\Source\Geotime\GeotimeConnectApiV2\GeotimeConnectWebApi\Controllers\Labores`.
Expected: todos `[Route("[controller]")]`, por lo que las rutas son `/PhLaboresBloque`, `/PhLaboresFinca`, `/PhLaboresLote`, `/PhLaboresOrdenTrabajo`, `/PhLaboresActividad`, `/PhLaboresUnidad`, `/PhLaboresRecurso`. Si alguna difiere, usar la real en el Step 3.

- [ ] **Step 2: Declarar los métodos en la interfaz**

Agregar en `IGeoTimeConnect.cs`, dentro de la interfaz:

```csharp
        // ── Catálogos de labores (Ph_Labores*) ───────────────────────────────
        // Devuelven null cuando la API no respondió con éxito (distinto de lista vacía).

        /// <summary>GetLaboresBloques: lista los bloques de labores.</summary>
        Task<IEnumerable<cPh_LaboresBloque>?> GetLaboresBloques();

        /// <summary>GetLaboresFincas: lista las fincas; opcionalmente de un bloque.</summary>
        Task<IEnumerable<cPh_LaboresFinca>?> GetLaboresFincas(string? bloqueId = null);

        /// <summary>GetLaboresLotes: lista los lotes; opcionalmente por bloque y/o finca.</summary>
        Task<IEnumerable<cPh_LaboresLote>?> GetLaboresLotes(string? bloqueId = null, string? fincaId = null);

        /// <summary>GetLaboresOrdenesTrabajo: lista las órdenes de trabajo; opcionalmente por bloque y/o finca.</summary>
        Task<IEnumerable<cPh_LaboresOrdenTrabajo>?> GetLaboresOrdenesTrabajo(string? bloqueId = null, string? fincaId = null);

        /// <summary>GetLaboresActividades: lista las actividades; opcionalmente por bloque, finca y/u orden de trabajo.</summary>
        Task<IEnumerable<cPh_LaboresActividad>?> GetLaboresActividades(string? bloqueId = null, string? fincaId = null, string? ordenTrabajoId = null);

        /// <summary>GetLaboresUnidades: lista las unidades de medida.</summary>
        Task<IEnumerable<cPh_LaboresUnidad>?> GetLaboresUnidades();

        /// <summary>GetLaboresRecursos: lista los recursos; opcionalmente por unidad y/o concepto.</summary>
        Task<IEnumerable<cPh_LaboresRecurso>?> GetLaboresRecursos(string? unidadId = null, int? idConcepto = null);
```

- [ ] **Step 3: Implementarlos en `GeoTimeConnect`**

Agregar después de `GetCentroCosto` (los `using` de `com.gsitcr.geotime.Models` y `System.Net.Http.Json` ya existen en el archivo):

```csharp
        // ── Catálogos de labores (Ph_Labores*) ───────────────────────────────

        /// <summary>
        /// GetListaLabores: GET de un catálogo. Devuelve null si la respuesta no fue exitosa
        /// o si hubo excepción, para que el llamador distinga "falló" de "vacío".
        /// </summary>
        private async Task<IEnumerable<T>?> GetListaLabores<T>(string method)
        {
            try
            {
                var result = await Get(method);
                if (!result.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"GetListaLabores({method}): {(int)result.StatusCode} {result.ReasonPhrase}");
                    return null;
                }

                return await result.Content.ReadFromJsonAsync<List<T>>();
            }
            catch (Exception e)
            {
                _logger.LogError($"GetListaLabores({method}): {e.Message}");
                return null;
            }
        }

        /// <summary>Arma "/Ruta?clave=valor&amp;..." omitiendo los filtros nulos o vacíos.</summary>
        private static string ConFiltros(string ruta, params (string clave, string? valor)[] filtros)
        {
            var partes = filtros
                .Where(f => !string.IsNullOrEmpty(f.valor))
                .Select(f => $"{f.clave}={Uri.EscapeDataString(f.valor!)}")
                .ToList();
            return partes.Count == 0 ? ruta : $"{ruta}?{string.Join("&", partes)}";
        }

        public Task<IEnumerable<cPh_LaboresBloque>?> GetLaboresBloques()
            => GetListaLabores<cPh_LaboresBloque>("/PhLaboresBloque");

        public Task<IEnumerable<cPh_LaboresFinca>?> GetLaboresFincas(string? bloqueId = null)
            => GetListaLabores<cPh_LaboresFinca>(ConFiltros("/PhLaboresFinca", ("bloqueId", bloqueId)));

        public Task<IEnumerable<cPh_LaboresLote>?> GetLaboresLotes(string? bloqueId = null, string? fincaId = null)
            => GetListaLabores<cPh_LaboresLote>(ConFiltros("/PhLaboresLote", ("bloqueId", bloqueId), ("fincaId", fincaId)));

        public Task<IEnumerable<cPh_LaboresOrdenTrabajo>?> GetLaboresOrdenesTrabajo(string? bloqueId = null, string? fincaId = null)
            => GetListaLabores<cPh_LaboresOrdenTrabajo>(ConFiltros("/PhLaboresOrdenTrabajo", ("bloqueId", bloqueId), ("fincaId", fincaId)));

        public Task<IEnumerable<cPh_LaboresActividad>?> GetLaboresActividades(string? bloqueId = null, string? fincaId = null, string? ordenTrabajoId = null)
            => GetListaLabores<cPh_LaboresActividad>(ConFiltros("/PhLaboresActividad", ("bloqueId", bloqueId), ("fincaId", fincaId), ("ordenTrabajoId", ordenTrabajoId)));

        public Task<IEnumerable<cPh_LaboresUnidad>?> GetLaboresUnidades()
            => GetListaLabores<cPh_LaboresUnidad>("/PhLaboresUnidad");

        public Task<IEnumerable<cPh_LaboresRecurso>?> GetLaboresRecursos(string? unidadId = null, int? idConcepto = null)
            => GetListaLabores<cPh_LaboresRecurso>(ConFiltros("/PhLaboresRecurso", ("unidadId", unidadId), ("idConcepto", idConcepto?.ToString())));
```

`List<T>` se devuelve como `IEnumerable<T>?` por covarianza implícita; si el compilador se queja de `Task<IEnumerable<T>?>` vs `Task<List<T>?>`, en `GetListaLabores` hacer `var lista = await result.Content.ReadFromJsonAsync<List<T>>(); return lista;`.

- [ ] **Step 4: Compilar**

Run: el comando de compilación de Global Constraints.
Expected: `Build succeeded` (puede haber advertencias previas). Si falla porque otra clase implementa `IGeoTimeConnect` (p. ej. un mock), agregarle los 7 métodos con `=> Task.FromResult<IEnumerable<...>?>(null)`.

- [ ] **Step 5: Commit**

```bash
git add LaboresNet/Data/Interfaces/IGeoTimeConnect.cs LaboresNet/Data/GeoTimeConnect.cs
git commit -m "feat: métodos de lectura de catálogos Ph_Labores* en GeoTimeConnect"
```

---

## Task 2: Base local `LaboresAGR` y tablas de catálogo

**Files:**
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs` (constructor, líneas 17-180; nuevos métodos antes de la región `// TicoFrut — GeoAgrFinca`, línea ~2597)
- Modify: `LaboresNet/Data/DataBaseLocal/LocalEntities.cs` (agregar entidades después de la línea 330)

**Interfaces:**
- Consumes: modelos `cPh_Labores*` (namespace `com.gsitcr.geotime.Models`, ya importado en `MarcasDBLocal.cs`).
- Produces (los usa Task 3):
  ```csharp
  public const string NombreBaseLocal = "LaboresAGR";
  Task<List<cPh_LaboresBloque>>       GetLaboresBloquesAsync();
  Task<List<cPh_LaboresFinca>>        GetLaboresFincasAsync();
  Task<List<cPh_LaboresLote>>         GetLaboresLotesAsync();
  Task<List<cPh_LaboresOrdenTrabajo>> GetLaboresOrdenesTrabajoAsync();
  Task<List<cPh_LaboresActividad>>    GetLaboresActividadesAsync();
  Task<List<cPh_LaboresUnidad>>       GetLaboresUnidadesAsync();
  Task<List<cPh_LaboresRecurso>>      GetLaboresRecursosAsync();
  Task SaveLaboresCatalogosAsync(IEnumerable<cPh_LaboresBloque> bloques, IEnumerable<cPh_LaboresFinca> fincas,
        IEnumerable<cPh_LaboresLote> lotes, IEnumerable<cPh_LaboresOrdenTrabajo> ordenes,
        IEnumerable<cPh_LaboresActividad> actividades, IEnumerable<cPh_LaboresUnidad> unidades,
        IEnumerable<cPh_LaboresRecurso> recursos);
  ```
  `SaveLaboresCatalogosAsync` reemplaza las 7 tablas en **una sola transacción**.

Las tablas y métodos viejos `GeoAgrFinca/Lote/Ot/OtLabor/RecursoConcepto` siguen existiendo hasta Task 3 (los consumidores aún los usan); en Task 3 se eliminan.

- [ ] **Step 1: Agregar las entidades locales**

En `LocalEntities.cs`, después de `cParametroApiLocal` (línea ~330) y antes del comentario `// ─── TicoFrut local entities`:

```csharp
    // ─── Catálogos de labores (Ph_Labores*) ─────────────────────────────────────
    // Las PKs son compuestas; las tablas se crean con SQL raw en MarcasDBLocal.

    [Table("LaboresBloque")]
    public class LaboresBloqueLocal
    {
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresFinca")]
    public class LaboresFincaLocal
    {
        public string BloqueId { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresLote")]
    public class LaboresLoteLocal
    {
        public string BloqueId { get; set; } = "";
        public string FincaId { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresOrdenTrabajo")]
    public class LaboresOrdenTrabajoLocal
    {
        public string BloqueId { get; set; } = "";
        public string FincaId { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresActividad")]
    public class LaboresActividadLocal
    {
        public string BloqueId { get; set; } = "";
        public string FincaId { get; set; } = "";
        public string OrdenTrabajoId { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresUnidad")]
    public class LaboresUnidadLocal
    {
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
    }

    [Table("LaboresRecurso")]
    public class LaboresRecursoLocal
    {
        public string Id { get; set; } = "";
        public string? Descripcion { get; set; }
        public int IdConcepto { get; set; }
        public string UnidadId { get; set; } = "";
    }
```

- [ ] **Step 2: Cambiar la ruta de la base**

En el constructor de `MarcasDBLocal` (líneas 21-23) reemplazar:

```csharp
                string dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AvancesMoDB");
                Directory.CreateDirectory(dbFolder);
                string dbPath = Path.Combine(dbFolder, "LaboresDatabase.db3");
```

por:

```csharp
                string dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), NombreBaseLocal);
                Directory.CreateDirectory(dbFolder);
                string dbPath = Path.Combine(dbFolder, $"{NombreBaseLocal}.db3");
```

y declarar, junto a `public static MarcasDBLocal instance;`:

```csharp
        /// <summary>Nombre de la base local (carpeta y archivo .db3). Distinto de la base anterior "AvancesMoDB\LaboresDatabase.db3".</summary>
        public const string NombreBaseLocal = "LaboresAGR";
```

- [ ] **Step 3: Crear las tablas de catálogo**

En el constructor, justo antes del comentario `// ── TicoFrut tables ──` (línea 74), insertar:

```csharp
                // ── Catálogos de labores (Geotime: Ph_Labores*) ─────────────────
                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS LaboresBloque (
                    Id          TEXT NOT NULL,
                    Descripcion TEXT,
                    PRIMARY KEY (Id)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS LaboresFinca (
                    BloqueId    TEXT NOT NULL,
                    Id          TEXT NOT NULL,
                    Descripcion TEXT,
                    PRIMARY KEY (BloqueId, Id)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS LaboresLote (
                    BloqueId    TEXT NOT NULL,
                    FincaId     TEXT NOT NULL,
                    Id          TEXT NOT NULL,
                    Descripcion TEXT,
                    PRIMARY KEY (BloqueId, FincaId, Id)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS LaboresOrdenTrabajo (
                    BloqueId    TEXT NOT NULL,
                    FincaId     TEXT NOT NULL,
                    Id          TEXT NOT NULL,
                    Descripcion TEXT,
                    PRIMARY KEY (BloqueId, FincaId, Id)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS LaboresActividad (
                    BloqueId       TEXT NOT NULL,
                    FincaId        TEXT NOT NULL,
                    OrdenTrabajoId TEXT NOT NULL,
                    Id             TEXT NOT NULL,
                    Descripcion    TEXT,
                    PRIMARY KEY (BloqueId, FincaId, OrdenTrabajoId, Id)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS LaboresUnidad (
                    Id          TEXT NOT NULL,
                    Descripcion TEXT,
                    PRIMARY KEY (Id)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS LaboresRecurso (
                    Id          TEXT NOT NULL,
                    Descripcion TEXT,
                    IdConcepto  INTEGER NOT NULL,
                    UnidadId    TEXT NOT NULL,
                    PRIMARY KEY (Id)
                );").Wait();
```

- [ ] **Step 4: Agregar lectura y reemplazo**

En `MarcasDBLocal`, antes de la región `// TicoFrut — GeoAgrFinca` (~línea 2597):

```csharp
        // ════════════════════════════════════════════════════════════════════════
        // Catálogos de labores (Geotime: Ph_Labores*)
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<cPh_LaboresBloque>> GetLaboresBloquesAsync()
        {
            var locals = await database.Table<LaboresBloqueLocal>().ToListAsync();
            return locals.Select(l => new cPh_LaboresBloque { Id = l.Id, Descripcion = l.Descripcion ?? "", Estado = true })
                         .OrderBy(l => l.Id).ToList();
        }

        public async Task<List<cPh_LaboresFinca>> GetLaboresFincasAsync()
        {
            var locals = await database.Table<LaboresFincaLocal>().ToListAsync();
            return locals.Select(l => new cPh_LaboresFinca { BloqueId = l.BloqueId, Id = l.Id, Descripcion = l.Descripcion ?? "", Estado = true })
                         .OrderBy(l => l.BloqueId).ThenBy(l => l.Id).ToList();
        }

        public async Task<List<cPh_LaboresLote>> GetLaboresLotesAsync()
        {
            var locals = await database.Table<LaboresLoteLocal>().ToListAsync();
            return locals.Select(l => new cPh_LaboresLote { BloqueId = l.BloqueId, FincaId = l.FincaId, Id = l.Id, Descripcion = l.Descripcion ?? "", Estado = true })
                         .OrderBy(l => l.BloqueId).ThenBy(l => l.FincaId).ThenBy(l => l.Id).ToList();
        }

        public async Task<List<cPh_LaboresOrdenTrabajo>> GetLaboresOrdenesTrabajoAsync()
        {
            var locals = await database.Table<LaboresOrdenTrabajoLocal>().ToListAsync();
            return locals.Select(l => new cPh_LaboresOrdenTrabajo { BloqueId = l.BloqueId, FincaId = l.FincaId, Id = l.Id, Descripcion = l.Descripcion ?? "", Estado = true })
                         .OrderBy(l => l.BloqueId).ThenBy(l => l.FincaId).ThenBy(l => l.Id).ToList();
        }

        public async Task<List<cPh_LaboresActividad>> GetLaboresActividadesAsync()
        {
            var locals = await database.Table<LaboresActividadLocal>().ToListAsync();
            return locals.Select(l => new cPh_LaboresActividad { BloqueId = l.BloqueId, FincaId = l.FincaId, OrdenTrabajoId = l.OrdenTrabajoId, Id = l.Id, Descripcion = l.Descripcion ?? "", Estado = true })
                         .OrderBy(l => l.BloqueId).ThenBy(l => l.FincaId).ThenBy(l => l.OrdenTrabajoId).ThenBy(l => l.Id).ToList();
        }

        public async Task<List<cPh_LaboresUnidad>> GetLaboresUnidadesAsync()
        {
            var locals = await database.Table<LaboresUnidadLocal>().ToListAsync();
            return locals.Select(l => new cPh_LaboresUnidad { Id = l.Id, Descripcion = l.Descripcion ?? "", Estado = true })
                         .OrderBy(l => l.Id).ToList();
        }

        public async Task<List<cPh_LaboresRecurso>> GetLaboresRecursosAsync()
        {
            var locals = await database.Table<LaboresRecursoLocal>().ToListAsync();
            return locals.Select(l => new cPh_LaboresRecurso { Id = l.Id, Descripcion = l.Descripcion ?? "", IdConcepto = l.IdConcepto, UnidadId = l.UnidadId, Estado = true })
                         .OrderBy(l => l.Descripcion).ToList();
        }

        /// <summary>
        /// Reemplaza los siete catálogos en una sola transacción: o quedan todos actualizados o ninguno.
        /// Solo se guardan los registros con Estado = true.
        /// </summary>
        public async Task SaveLaboresCatalogosAsync(
            IEnumerable<cPh_LaboresBloque> bloques,
            IEnumerable<cPh_LaboresFinca> fincas,
            IEnumerable<cPh_LaboresLote> lotes,
            IEnumerable<cPh_LaboresOrdenTrabajo> ordenes,
            IEnumerable<cPh_LaboresActividad> actividades,
            IEnumerable<cPh_LaboresUnidad> unidades,
            IEnumerable<cPh_LaboresRecurso> recursos)
        {
            var lBloques = bloques.Where(x => x.Estado)
                .Select(x => new LaboresBloqueLocal { Id = x.Id, Descripcion = x.Descripcion }).ToList();
            var lFincas = fincas.Where(x => x.Estado)
                .Select(x => new LaboresFincaLocal { BloqueId = x.BloqueId, Id = x.Id, Descripcion = x.Descripcion }).ToList();
            var lLotes = lotes.Where(x => x.Estado)
                .Select(x => new LaboresLoteLocal { BloqueId = x.BloqueId, FincaId = x.FincaId, Id = x.Id, Descripcion = x.Descripcion }).ToList();
            var lOrdenes = ordenes.Where(x => x.Estado)
                .Select(x => new LaboresOrdenTrabajoLocal { BloqueId = x.BloqueId, FincaId = x.FincaId, Id = x.Id, Descripcion = x.Descripcion }).ToList();
            var lActividades = actividades.Where(x => x.Estado)
                .Select(x => new LaboresActividadLocal { BloqueId = x.BloqueId, FincaId = x.FincaId, OrdenTrabajoId = x.OrdenTrabajoId, Id = x.Id, Descripcion = x.Descripcion }).ToList();
            var lUnidades = unidades.Where(x => x.Estado)
                .Select(x => new LaboresUnidadLocal { Id = x.Id, Descripcion = x.Descripcion }).ToList();
            var lRecursos = recursos.Where(x => x.Estado)
                .Select(x => new LaboresRecursoLocal { Id = x.Id, Descripcion = x.Descripcion, IdConcepto = x.IdConcepto, UnidadId = x.UnidadId }).ToList();

            await database.RunInTransactionAsync(db =>
            {
                db.DeleteAll<LaboresBloqueLocal>();
                db.DeleteAll<LaboresFincaLocal>();
                db.DeleteAll<LaboresLoteLocal>();
                db.DeleteAll<LaboresOrdenTrabajoLocal>();
                db.DeleteAll<LaboresActividadLocal>();
                db.DeleteAll<LaboresUnidadLocal>();
                db.DeleteAll<LaboresRecursoLocal>();

                db.InsertAll(lBloques);
                db.InsertAll(lFincas);
                db.InsertAll(lLotes);
                db.InsertAll(lOrdenes);
                db.InsertAll(lActividades);
                db.InsertAll(lUnidades);
                db.InsertAll(lRecursos);
            });
        }
```

- [ ] **Step 5: Compilar**

Run: comando de compilación de Global Constraints.
Expected: `Build succeeded`.

- [ ] **Step 6: Verificar que se crea la base nueva**

Ejecutar la app (`dotnet run` del proyecto Windows, o F5 en el IDE), llegar a la pantalla de login y cerrarla.
Run: `Get-ChildItem "$env:LOCALAPPDATA\LaboresAGR"`
Expected: existe `LaboresAGR.db3`. La carpeta `AvancesMoDB` no cambió de fecha de modificación.

- [ ] **Step 7: Commit**

```bash
git add LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs LaboresNet/Data/DataBaseLocal/LocalEntities.cs
git commit -m "feat: base local LaboresAGR y tablas de catálogo Labores*"
```

---

## Task 3: Sincronización y consumo de catálogos Geotime (UI)

Cambia los consumidores de los 5 catálogos Ticofrut a los 7 catálogos de Geotime y elimina la dependencia de `GeoAgrFinca/Lote/Ot/OtLabor/RecursoConcepto`. `Compania` en los catálogos desaparece aquí; en las labores auxiliares, en Task 4.

**Files:**
- Create: `LaboresNet/Data/LaboresCatalogoSync.cs`
- Modify: `LaboresNet/Components/Pages/Principal.razor:71-76, 122-132, 148-152, 195-199`
- Modify: `LaboresNet/Components/Pages/Procesos/SincronizacionManual.razor:96-114, 139-151, 167-171, 213-217`
- Modify: `LaboresNet/Components/Pages/ComponentesPop/GeoAgrFincaPop.razor`, `GeoAgrLotePop.razor`, `GeoAgrOtPop.razor`, `GeoAgrOtLaborPop.razor`, `GeoAgrRecursoConceptoPop.razor`
- Modify: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor:300-304, 386-405, 597-821`
- Modify: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarEdicion.razor:246-254, 286-290`
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs` (eliminar la región TicoFrut de catálogos, líneas ~2597-3157, y las 5 tablas `GeoAgrFinca/Lote/Ot/OtLabor/RecursoConcepto` del constructor)
- Modify: `LaboresNet/Data/DataBaseLocal/LocalEntities.cs` (eliminar `GeoAgrFincaLocal`, `GeoAgrLoteLocal`, `GeoAgrOtLocal`, `GeoAgrOtLaborLocal`, `GeoAgrRecursoConceptoLocal`)

**Interfaces:**
- Consumes: `IGeoTimeConnect.GetLabores*` (Task 1), `MarcasDBLocal.GetLabores*Async` y `SaveLaboresCatalogosAsync` (Task 2).
- Produces:
  ```csharp
  // namespace LaboresNet.Data
  public static class LaboresCatalogoSync
  {
      /// true si descargó y guardó los 7 catálogos; false si alguno falló (no se toca nada local).
      public static Task<bool> SincronizarAsync(IGeoTimeConnect geo, MarcasDBLocal db);
  }
  ```
  Parámetros de los pops: `GeoAgrFincaPop.fincas : IEnumerable<cPh_LaboresFinca>?`, `GeoAgrLotePop.lotes : IEnumerable<cPh_LaboresLote>?`, `GeoAgrOtPop.ots : IEnumerable<cPh_LaboresOrdenTrabajo>?`, `GeoAgrOtLaborPop.otLabores : IEnumerable<cPh_LaboresActividad>?`, `GeoAgrRecursoConceptoPop.recursos : IEnumerable<cPh_LaboresRecurso>?`; y los `EventCallback` con los mismos tipos. Los nombres de los parámetros y de los eventos no cambian.

- [ ] **Step 1: Crear el helper de sincronización**

Create `LaboresNet/Data/LaboresCatalogoSync.cs` (usar el mismo namespace y `using` de `IGeoTimeConnect`; abrir `Data/Interfaces/IGeoTimeConnect.cs` para copiarlos):

```csharp
using com.gsitcr.geotime.Models;
using LaboresNet.Data.Interfaces;
using PortalEmpleadoGeotime.Client.Data.DataBaseLocal;

namespace LaboresNet.Data
{
    /// <summary>
    /// Descarga los catálogos de labores (Ph_Labores*) de la API de Geotime y los guarda en la base local.
    /// Es todo o nada: si algún catálogo no responde, no se modifica nada local.
    /// </summary>
    public static class LaboresCatalogoSync
    {
        public static async Task<bool> SincronizarAsync(IGeoTimeConnect geo, MarcasDBLocal db)
        {
            var tBloques = geo.GetLaboresBloques();
            var tFincas = geo.GetLaboresFincas();
            var tLotes = geo.GetLaboresLotes();
            var tOrdenes = geo.GetLaboresOrdenesTrabajo();
            var tActividades = geo.GetLaboresActividades();
            var tUnidades = geo.GetLaboresUnidades();
            var tRecursos = geo.GetLaboresRecursos();

            await Task.WhenAll(tBloques, tFincas, tLotes, tOrdenes, tActividades, tUnidades, tRecursos);

            var bloques = tBloques.Result;
            var fincas = tFincas.Result;
            var lotes = tLotes.Result;
            var ordenes = tOrdenes.Result;
            var actividades = tActividades.Result;
            var unidades = tUnidades.Result;
            var recursos = tRecursos.Result;

            // null = la API falló; una lista vacía es una respuesta válida.
            if (bloques is null || fincas is null || lotes is null || ordenes is null
                || actividades is null || unidades is null || recursos is null)
                return false;

            await db.SaveLaboresCatalogosAsync(bloques, fincas, lotes, ordenes, actividades, unidades, recursos);
            return true;
        }
    }
}
```

- [ ] **Step 2: `Principal.razor` — quitar Ticofrut y sincronizar catálogos Geotime**

Reemplazar las líneas 71-75:

```razor
            // ── Sincronización TicoFrut ──────────────────────────────────────
            if (await ticoFrutConnect.ApiDisponible())
            {
                await SincronizarBases();
            }     
```

por:

```razor
            await SincronizarBases();
```

Eliminar las líneas 122-126 (`fincasTask` … `recursosTask`), y quitar `fincasTask, lotesTask, otsTask, otLaboresTask, recursosTask` de `Task.WhenAll` (líneas 128-132; dejar `laborAuxiliarTask` una sola vez), y eliminar las líneas 148-152 (`var fincas = …` … `var recursos = …`). Reemplazar las líneas 195-199:

```csharp
            await marcaDBLocal.SaveGeoAgrFincaAsync(fincas);
            await marcaDBLocal.SaveGeoAgrLoteAsync(lotes);
            await marcaDBLocal.SaveGeoAgrOtAsync(ots);
            await marcaDBLocal.SaveGeoAgrOtLaborAsync(otLabores);
            await marcaDBLocal.SaveGeoAgrRecursoConceptoAsync(recursos);
```

por:

```csharp
            // Catálogos de labores desde Geotime (todo o nada)
            if (!await LaboresCatalogoSync.SincronizarAsync(geoConnect, marcaDBLocal))
            {
                await swal.FireAsync("Atención", "No se pudieron actualizar los catálogos de labores. Se mantienen los datos locales.", SweetAlertIcon.Warning);
            }
```

Agregar `@using LaboresNet.Data` al inicio del archivo (junto al `@using` existente de `DataBaseLocal`).

- [ ] **Step 3: `SincronizacionManual.razor` — mismo cambio**

Reemplazar `SincronizarDatos` (líneas 94-114) por:

```csharp
	private async Task SincronizarDatos()
	{
		InProcess = true;
		if (await geoConnect.ApiDisponible())
		{
			await SincronizarBases();
		}
		else
		{
			await swal.FireAsync("Atención", $"En este momento no hay comunicación con la base de datos central.", SweetAlertIcon.Warning);
		}
		InProcess=false;
		StateHasChanged();
	}
```

Eliminar las líneas 139-144 (bloque "Sincronización TicoFrut"), quitar `fincasTask, lotesTask, otsTask, otLaboresTask, recursosTask` del `Task.WhenAll` (líneas 147-151), eliminar las líneas 167-171 y reemplazar las líneas 213-217 (`SaveGeoAgrFincaAsync` … `SaveGeoAgrRecursoConceptoAsync`) por el mismo bloque `LaboresCatalogoSync.SincronizarAsync(geoConnect, marcaDBLocal)` del Step 2. Agregar `@using LaboresNet.Data`.

- [ ] **Step 4: Pops — usar los modelos `cPh_Labores*`**

`GeoAgrFincaPop.razor` (reemplazo completo):

```razor
@if (fincas is null || !fincas.Any())
{
    <div style="text-align:center;padding:24px 0;color:#999;font-size:0.85rem;">
        No hay fincas disponibles. Sincronice los datos para descargar los catálogos.
    </div>
}
else
{
<RadzenDataGrid TItem="cPh_LaboresFinca" Data="@fincas"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cPh_LaboresFinca" Property="Descripcion" Title="Finca">
            <Template Context="item">
                <div style="display:flex;gap:8px;align-items:flex-start;padding:2px 0;">
                    <div class="emp-avatar" style="width:36px;height:36px;font-size:0.85rem;flex-shrink:0;">
                        @(string.IsNullOrEmpty(item.Descripcion) ? "?" : item.Descripcion.Substring(0, 1).ToUpper())
                    </div>
                    <div style="flex:1;min-width:0;">
                        <div style="font-weight:600;font-size:0.85rem;color:#212121;line-height:1.3;">@item.Descripcion</div>
                        <div class="emp-meta-row">
                            <RadzenIcon Icon="tag" Style="font-size:0.78rem;color:#bbb;flex-shrink:0;" />
                            @item.Id
                        </div>
                        @if (!string.IsNullOrEmpty(item.BloqueId))
                        {
                            <div class="emp-meta-row">
                                <RadzenIcon Icon="map" Style="font-size:0.78rem;color:#bbb;flex-shrink:0;" />
                                Bloque @item.BloqueId
                            </div>
                        }
                    </div>
                </div>
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>
}

@code {
    [Parameter] public EventCallback<cPh_LaboresFinca> OnGeoAgrFincaSelected { get; set; }
    [Parameter] public IEnumerable<cPh_LaboresFinca>? fincas { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<cPh_LaboresFinca> args)
    {
        OnGeoAgrFincaSelected.InvokeAsync(args.Data);
    }
}
```

`GeoAgrLotePop.razor` (reemplazo completo):

```razor
@if (lotes is null || !lotes.Any())
{
    <div style="text-align:center;padding:24px 0;color:#999;font-size:0.85rem;">
        No hay lotes para la finca seleccionada. Sincronice los datos para descargar los catálogos.
    </div>
}
else
{
<RadzenDataGrid TItem="cPh_LaboresLote" Data="@lotes"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cPh_LaboresLote" Property="Id" Title="Lote">
            <Template Context="item">
                <div style="display:flex;gap:8px;align-items:flex-start;padding:2px 0;">
                    <div class="emp-avatar" style="width:36px;height:36px;font-size:0.85rem;flex-shrink:0;">
                        @(string.IsNullOrEmpty(item.Id) ? "?" : item.Id.Substring(0, 1).ToUpper())
                    </div>
                    <div style="flex:1;min-width:0;">
                        <div style="font-weight:600;font-size:0.85rem;color:#212121;line-height:1.3;">@item.Id</div>
                        @if (!string.IsNullOrEmpty(item.Descripcion))
                        {
                            <div class="emp-meta-row">
                                <RadzenIcon Icon="terrain" Style="font-size:0.78rem;color:#bbb;flex-shrink:0;" />
                                @item.Descripcion
                            </div>
                        }
                    </div>
                </div>
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>
}

@code {
    [Parameter] public EventCallback<cPh_LaboresLote> OnGeoAgrLoteSelected { get; set; }
    [Parameter] public IEnumerable<cPh_LaboresLote>? lotes { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<cPh_LaboresLote> args)
    {
        OnGeoAgrLoteSelected.InvokeAsync(args.Data);
    }
}
```

`GeoAgrOtPop.razor` (reemplazo completo):

```razor
@if (ots is null || !ots.Any())
{
    <div style="text-align:center;padding:24px 0;color:#999;font-size:0.85rem;">
        No hay órdenes de trabajo para la finca seleccionada. Sincronice los datos para descargar los catálogos.
    </div>
}
else
{
<RadzenDataGrid TItem="cPh_LaboresOrdenTrabajo" Data="@ots"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cPh_LaboresOrdenTrabajo" Property="Descripcion" Title="Orden de Trabajo">
            <Template Context="item">
                <div style="display:flex;gap:8px;align-items:flex-start;padding:2px 0;">
                    <div class="emp-avatar" style="width:36px;height:36px;font-size:0.85rem;flex-shrink:0;">
                        @(string.IsNullOrEmpty(item.Descripcion) ? "?" : item.Descripcion.Substring(0, 1).ToUpper())
                    </div>
                    <div style="flex:1;min-width:0;">
                        <div style="font-weight:600;font-size:0.85rem;color:#212121;line-height:1.3;">@item.Descripcion</div>
                        @if (!string.IsNullOrEmpty(item.Id))
                        {
                            <div class="emp-meta-row">
                                <RadzenIcon Icon="tag" Style="font-size:0.78rem;color:#bbb;flex-shrink:0;" />
                                @item.Id
                            </div>
                        }
                    </div>
                </div>
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>
}

@code {
    [Parameter] public EventCallback<cPh_LaboresOrdenTrabajo> OnGeoAgrOtSelected { get; set; }
    [Parameter] public IEnumerable<cPh_LaboresOrdenTrabajo>? ots { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<cPh_LaboresOrdenTrabajo> args)
    {
        OnGeoAgrOtSelected.InvokeAsync(args.Data);
    }
}
```

`GeoAgrOtLaborPop.razor` (reemplazo completo):

```razor
@if (otLabores is null || !otLabores.Any())
{
    <div style="text-align:center;padding:24px 0;color:#999;font-size:0.85rem;">
        No hay labores para la orden de trabajo seleccionada. Sincronice los datos para descargar los catálogos.
    </div>
}
else
{
<RadzenDataGrid TItem="cPh_LaboresActividad" Data="@otLabores"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cPh_LaboresActividad" Property="Descripcion" Title="Labor">
            <Template Context="item">
                <div style="display:flex;gap:8px;align-items:flex-start;padding:2px 0;">
                    <div class="emp-avatar" style="width:36px;height:36px;font-size:0.85rem;flex-shrink:0;">
                        @(string.IsNullOrEmpty(item.Descripcion) ? "?" : item.Descripcion.Substring(0, 1).ToUpper())
                    </div>
                    <div style="flex:1;min-width:0;">
                        <div style="font-weight:600;font-size:0.85rem;color:#212121;line-height:1.3;">@item.Descripcion</div>
                        @if (!string.IsNullOrEmpty(item.Id))
                        {
                            <div class="emp-meta-row">
                                <RadzenIcon Icon="work" Style="font-size:0.78rem;color:#bbb;flex-shrink:0;" />
                                @item.Id
                            </div>
                        }
                    </div>
                </div>
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>
}

@code {
    [Parameter] public EventCallback<cPh_LaboresActividad> OnGeoAgrOtLaborSelected { get; set; }
    [Parameter] public IEnumerable<cPh_LaboresActividad>? otLabores { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<cPh_LaboresActividad> args)
    {
        OnGeoAgrOtLaborSelected.InvokeAsync(args.Data);
    }
}
```

`GeoAgrRecursoConceptoPop.razor` (reemplazo completo):

```razor
@if (recursos is null || !recursos.Any())
{
    <div style="text-align:center;padding:24px 0;color:#999;font-size:0.85rem;">
        No hay recursos disponibles. Sincronice los datos para descargar los catálogos.
    </div>
}
else
{
<RadzenDataGrid TItem="cPh_LaboresRecurso" Data="@recursos"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cPh_LaboresRecurso" Property="Descripcion" Title="Recurso / Concepto">
            <Template Context="item">
                <div style="display:flex;gap:8px;align-items:flex-start;padding:2px 0;">
                    <div class="emp-avatar" style="width:36px;height:36px;font-size:0.85rem;flex-shrink:0;">
                        @(string.IsNullOrEmpty(item.Descripcion) ? "?" : item.Descripcion.Substring(0, 1).ToUpper())
                    </div>
                    <div style="flex:1;min-width:0;">
                        <div style="font-weight:600;font-size:0.85rem;color:#212121;line-height:1.3;">@item.Descripcion</div>
                        @if (!string.IsNullOrEmpty(item.Id))
                        {
                            <div class="emp-meta-row">
                                <RadzenIcon Icon="inventory_2" Style="font-size:0.78rem;color:#bbb;flex-shrink:0;" />
                                @item.Id
                            </div>
                        }
                        @if (!string.IsNullOrEmpty(item.UnidadId))
                        {
                            <div class="emp-meta-row">
                                <RadzenIcon Icon="straighten" Style="font-size:0.78rem;color:#bbb;flex-shrink:0;" />
                                Unidad: @item.UnidadId
                            </div>
                        }
                    </div>
                </div>
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>
}

@code {
    [Parameter] public EventCallback<cPh_LaboresRecurso> OnGeoAgrRecursoConceptoSelected { get; set; }
    [Parameter] public IEnumerable<cPh_LaboresRecurso>? recursos { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<cPh_LaboresRecurso> args)
    {
        OnGeoAgrRecursoConceptoSelected.InvokeAsync(args.Data);
    }
}
```

- [ ] **Step 5: `GeoAgrLaborAuxiliarDetalle.razor` — catálogos y modales**

Líneas 300-304, reemplazar por:

```csharp
    private IEnumerable<cPh_LaboresFinca>? listaFincas = Enumerable.Empty<cPh_LaboresFinca>();
    private IEnumerable<cPh_LaboresLote>? listaLotes = Enumerable.Empty<cPh_LaboresLote>();
    private IEnumerable<cPh_LaboresOrdenTrabajo>? listaOts = Enumerable.Empty<cPh_LaboresOrdenTrabajo>();
    private IEnumerable<cPh_LaboresActividad>? listaOtLabores = Enumerable.Empty<cPh_LaboresActividad>();
    private IEnumerable<cPh_LaboresRecurso>? listaRecursoConcepto = Enumerable.Empty<cPh_LaboresRecurso>();
```

En `OnInitializedAsync` (líneas 390-394 y 400-404) reemplazar las cinco llamadas por:

```csharp
            var tFincas    = marcaDBLocal.GetLaboresFincasAsync();
            var tLotes     = marcaDBLocal.GetLaboresLotesAsync();
            var tOts       = marcaDBLocal.GetLaboresOrdenesTrabajoAsync();
            var tOtLabores = marcaDBLocal.GetLaboresActividadesAsync();
            var tRecursoConcepto = marcaDBLocal.GetLaboresRecursosAsync();
```

(las asignaciones `listaFincas = tFincas.Result;` etc. se mantienen igual).

Reemplazar los cinco pares modal/`HideModal*` (líneas 599-821). Solo cambia lo que sigue; el resto de cada método (try/catch, `DialogOptions`) queda igual:

`OnShowModalFinca` — sin cambios salvo el tipo del callback:

```csharp
                    { "OnGeoAgrFincaSelected", EventCallback.Factory.Create<cPh_LaboresFinca>(this, HideModalFinca) },
                    { "fincas", listaFincas! }
```

```csharp
    private async Task HideModalFinca(cPh_LaboresFinca param)
    {
        if (detActivo is not null)
        {
            detActivo.Bloque = param.BloqueId;
            detActivo.Finca  = param.Id;
            // Al cambiar de finca, los datos dependientes ya no son válidos.
            detActivo.Lote = null;
            detActivo.Ot = null;
            detActivo.NombreOt = "";
            detActivo.Labor = null;
            detActivo.NombreLabor = "";
        }
        dialogService.Close(true);
        StateHasChanged();
    }
```

`OnShowModalLote`:

```csharp
            var lotesFiltrados = listaLotes?
                .Where(l => l.BloqueId == det.Bloque && l.FincaId == det.Finca)
                ?? Enumerable.Empty<cPh_LaboresLote>();
```
con `EventCallback.Factory.Create<cPh_LaboresLote>(this, HideModalLote)`, y:

```csharp
    private async Task HideModalLote(cPh_LaboresLote param)
    {
        if (detActivo is not null)
            detActivo.Lote = param.Id;
        dialogService.Close(true);
        StateHasChanged();
    }
```

`OnShowModalOt`:

```csharp
            var otsFiltradas = listaOts?
                .Where(l => l.BloqueId == det.Bloque && l.FincaId == det.Finca)
                ?? Enumerable.Empty<cPh_LaboresOrdenTrabajo>();
```
con `EventCallback.Factory.Create<cPh_LaboresOrdenTrabajo>(this, HideModalOt)`, y:

```csharp
    private async Task HideModalOt(cPh_LaboresOrdenTrabajo param)
    {
        if (detActivo is not null)
        {
            detActivo.Ot = param.Id;
            detActivo.NombreOt = param.Descripcion;
            // La actividad pertenece a una OT: se limpia al cambiar de OT.
            detActivo.Labor = null;
            detActivo.NombreLabor = "";
        }
        dialogService.Close(true);
        StateHasChanged();
    }
```

`OnShowModalOtLabor`:

```csharp
            var laboresFiltradas = listaOtLabores?
                .Where(l => l.BloqueId == det.Bloque && l.FincaId == det.Finca && l.OrdenTrabajoId == det.Ot)
                ?? Enumerable.Empty<cPh_LaboresActividad>();
```
con `EventCallback.Factory.Create<cPh_LaboresActividad>(this, HideModalOtLabor)`, y:

```csharp
    private async Task HideModalOtLabor(cPh_LaboresActividad param)
    {
        if (detActivo is not null)
        {
            detActivo.Labor = param.Id;
            detActivo.NombreLabor = param.Descripcion;
        }
        dialogService.Close(true);
        StateHasChanged();
    }
```

`OnShowModalRecurso`: eliminar el filtro por compañía:

```csharp
            var recursosFiltrados = listaRecursoConcepto ?? Enumerable.Empty<cPh_LaboresRecurso>();
```
con `EventCallback.Factory.Create<cPh_LaboresRecurso>(this, HideModalRecurso)`, y:

```csharp
    private async Task HideModalRecurso(cPh_LaboresRecurso param)
    {
        if (detActivo is not null)
        {
            detActivo.Recurso = param.Id;
            detActivo.Unidad = param.UnidadId;
            detActivo.NombreRecurso = param.Descripcion;
        }

        dialogService.Close(true);
        StateHasChanged();
    }
```

- [ ] **Step 6: `GeoAgrLaborAuxiliarEdicion.razor` — nombres desde los nuevos catálogos**

Líneas 250-252, reemplazar por:

```csharp
            var tOts       = marcaDBLocal.GetLaboresOrdenesTrabajoAsync();
            var tOtLabores = marcaDBLocal.GetLaboresActividadesAsync();
            var tRecursos  = marcaDBLocal.GetLaboresRecursosAsync();
```

Líneas 286-290, reemplazar las tres búsquedas por (llave completa, sin excepción si falta el código):

```csharp
                            NombreOt = tOts.Result?.FirstOrDefault(o => o.Id == primero.Ot && o.BloqueId == primero.Bloque && o.FincaId == primero.Finca)?.Descripcion ?? "",
                            Labor   = primero.Labor,
                            NombreLabor = tOtLabores.Result?.FirstOrDefault(l => l.Id == primero.Labor && l.BloqueId == primero.Bloque && l.FincaId == primero.Finca && l.OrdenTrabajoId == primero.Ot)?.Descripcion ?? "",
                            Recurso = primero.Recurso,
                            NombreRecurso = tRecursos.Result?.FirstOrDefault(r => r.Id == primero.Recurso)?.Descripcion ?? "",
```

(La línea 285 `Ot = primero.Ot,` se conserva.) Esta página aún pasa `compania` a `GetGeoAgrLaborAuxiliar*` y usa `loginState.CompaniaSeleccionada` en otras líneas: eso se elimina en Task 4, pero el `GetGeoAgrOtAsync(loginState.CompaniaSeleccionada)` de las líneas 250-252 ya fue reemplazado aquí.

- [ ] **Step 7: Eliminar el código Ticofrut de catálogos en la base local**

En `MarcasDBLocal.cs`:
1. Borrar el bloque `// ── TicoFrut tables ──` del constructor con las 5 sentencias `CREATE TABLE IF NOT EXISTS GeoAgrFinca/GeoAgrLote/GeoAgrOt/GeoAgrOtLabor/GeoAgrRecursoConcepto` y el comentario `//database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrRecursoConcepto")` (líneas 74-122). **Dejar** los `CREATE TABLE` de `GeoAgrLaborAuxiliar` y `GeoAgrLaborAuxiliarDet`.
2. Borrar las regiones `TicoFrut — GeoAgrFinca`, `GeoAgrLote`, `GeoAgrOt`, `GeoAgrOtLabor`, `GeoAgrRecursoConcepto` (desde el comentario `// TicoFrut — GeoAgrFinca`, ~línea 2597, hasta justo antes de `// TicoFrut — GeoAgrLaborAuxiliar`, ~línea 3159). **No** borrar `GeoAgrLaborAuxiliar` ni `GeoAgrLaborAuxiliarDet`.

En `LocalEntities.cs`: borrar `GeoAgrFincaLocal`, `GeoAgrLoteLocal`, `GeoAgrOtLocal`, `GeoAgrOtLaborLocal` y `GeoAgrRecursoConceptoLocal` (líneas 335-387).

- [ ] **Step 8: Compilar**

Run: comando de compilación de Global Constraints.
Expected: `Build succeeded`. Errores esperables y su arreglo:
- `GeoAgrFinca/Lote/Ot/OtLabor/RecursoConcepto` no encontrado en `Principal.razor`/`SincronizacionManual.razor`/`MainLayout.razor`: `MainLayout.razor` aún usa `GetGeoAgrFincaAsync` para los selectores País/Compañía; **se elimina en Task 6**. Para que el proyecto compile ahora, en `MainLayout.razor` reemplazar temporalmente `List<GeoAgrFinca> todasLasFincas = new();` y `await marcasDBLocal.GetGeoAgrFincaAsync()` por una lista vacía:
  `var todasLasFincas = new List<(string? Pais, string? Compania)>();` y quitar el `await`. Task 6 elimina todo ese bloque.

- [ ] **Step 9: Verificar sincronización contra la API**

Ejecutar la app, iniciar sesión, abrir **Sincronizar** → **Sincronizar Datos**.
Run (PowerShell, tras sincronizar; requiere `sqlite3` en PATH, o abrir el archivo con DB Browser):
`sqlite3 "$env:LOCALAPPDATA\LaboresAGR\LaboresAGR.db3" "select 'bloques',count(*) from LaboresBloque union all select 'fincas',count(*) from LaboresFinca union all select 'lotes',count(*) from LaboresLote union all select 'ots',count(*) from LaboresOrdenTrabajo union all select 'actividades',count(*) from LaboresActividad union all select 'unidades',count(*) from LaboresUnidad union all select 'recursos',count(*) from LaboresRecurso;"`
Expected: conteos > 0 que coincidan con `SELECT COUNT(*) FROM Ph_Labores... WHERE Estado = 1` en la base de Geotime (cuando el MCP `sqlserver` esté disponible, comparar). Probar también: detener la API y sincronizar → aparece el aviso y los conteos no cambian.

- [ ] **Step 10: Verificar la captura de una línea**

En **Registro de Labores Auxiliares → Agregar**: agregar línea, elegir Bloque/Finca → Lote → OT → Labor → Recurso. Expected: los pops listan datos; al elegir una OT se limpia la Labor; el campo Unidad se llena con el `UnidadId` del recurso; con la finca sin lotes el pop muestra el aviso.

- [ ] **Step 11: Commit**

```bash
git add LaboresNet
git commit -m "feat: catálogos de labores desde Geotime en sincronización y pantallas"
```

---

## Task 4: Quitar `Compania` de las labores auxiliares y usar el tenant de la sesión

**Files:**
- Modify: `LaboresNet/Data/State/LoginState.cs`
- Modify: `LaboresNet/Modelo/GeoAgrLaborAuxiliar.cs`, `LaboresNet/Modelo/GeoAgrLaborAuxiliarDet.cs`
- Modify: `LaboresNet/Data/DataBaseLocal/LocalEntities.cs` (`GeoAgrLaborAuxiliarLocal`, `GeoAgrLaborAuxiliarDetLocal`)
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs` (DDL y región `GeoAgrLaborAuxiliar` / `GeoAgrLaborAuxiliarDet`, líneas ~3158-3582 antes de eliminar catálogos; en el archivo ya modificado por Task 3 buscar el comentario `// TicoFrut — GeoAgrLaborAuxiliar`)
- Modify: `LaboresNet/Components/Pages/Home.razor:328, 402`
- Modify: `LaboresNet/Components/Pages/RelojMarcador/Marcador.razor:~92`
- Modify: `LaboresNet/Components/Pages/Principal.razor`, `LaboresNet/Components/Pages/Procesos/SincronizacionManual.razor`, `GeoAgrLaborAuxiliarList.razor`, `GeoAgrLaborAuxiliarEdicion.razor`, `GeoAgrLaborAuxiliarDetalle.razor`

**Interfaces:**
- Consumes: `cPh_Compania.IDCOMP` (tenant, ya disponible al hacer login).
- Produces:
  ```csharp
  // LoginState
  public string CompaniaTenant { get; set; }          // IDCOMP del login; "" si no hay sesión
  public void SetCompaniaTenant(string idcomp);
  // MarcasDBLocal (sin parámetro compania)
  Task<List<GeoAgrLaborAuxiliar>>     GetGeoAgrLaborAuxiliarAsync(string idEmpleado);
  Task<GeoAgrLaborAuxiliar?>          GetGeoAgrLaborAuxiliarAsync(string idEmpleado, DateTime fecha, long idRegistro);
  Task<List<GeoAgrLaborAuxiliar>>     GetGeoAgrLaborAuxiliarAsync(string idEmpleado, DateTime fechaInicio, DateTime fechaFinal);
  Task<int>                           SaveGeoAgrLaborAuxiliarEstadoAsync(long idRegistro);
  Task<List<GeoAgrLaborAuxiliarDet>>  GetGeoAgrLaborAuxiliarDetAsync(string idEmpleado);
  Task<List<GeoAgrLaborAuxiliarDet>>  GetGeoAgrLaborAuxiliarDetAsync(string idEmpleado, DateTime fechaInicio, DateTime fechaFinal);
  Task<List<GeoAgrLaborAuxiliarDet>>  GetGeoAgrLaborAuxiliarDetAsync(string idEmpleado, DateTime fecha, long idRegistro);
  Task<GeoAgrLaborAuxiliarDet?>       GetGeoAgrLaborAuxiliarDetPorItemAsync(string idEmpleado, DateTime fecha, int item);
  Task<bool> DeleteGeoAgrLaborAuxiliarAsync(string idEmpleado, DateTime fecha);
  Task<bool> DeleteGeoAgrLaborAuxiliarDetAsync(string idEmpleado, DateTime fecha);
  ```
  El modelo `GeoAgrLaborAuxiliar` y `GeoAgrLaborAuxiliarDet` pierden la propiedad `Compania`.

- [ ] **Step 1: `LoginState` — tenant en lugar de compañía/país seleccionados**

En `LoginState.cs` reemplazar las líneas 20-21 y los métodos `SetCompania`/`SetPais` (líneas 42-52):

```csharp
        /// <summary>
        /// IDCOMP de la compañía con que se inició sesión. Solo identifica el tenant de la API
        /// de Geotime; no es un dato de negocio ni filtra catálogos.
        /// </summary>
        public string CompaniaTenant { get; set; } = "";
```

```csharp
        public void SetCompaniaTenant(string idcomp)
        {
            CompaniaTenant = idcomp ?? "";
            NotifyStateChanged();
        }
```

(Eliminar `CompaniaSeleccionada`, `PaisSeleccionado`, `SetCompania`, `SetPais`. Los usos que queden se corrigen en las demás tareas; el compilador los señalará.)

- [ ] **Step 2: Fijar el tenant en login y marcador**

`Home.razor`, después de `geoConnect.setSchema(companiaSeleccionada);` (línea 328, login online) y de `geoConnect.setSchema(companiaSeleccionada!);` (línea 402, login offline) agregar:

```csharp
                loginState.SetCompaniaTenant(companiaSeleccionada.IDCOMP);
```
(en la rama offline, con `companiaSeleccionada!.IDCOMP`).

`Marcador.razor`, dentro de `SeleccionarCompania`, después de `geoConnect.setSchema(companiaSeleccionada!);` (línea 92):

```csharp
            loginState.SetCompaniaTenant(companiaSeleccionada!.IDCOMP);
```

- [ ] **Step 3: Modelos sin `Compania`**

En `Modelo/GeoAgrLaborAuxiliar.cs` eliminar:

```csharp
        [Column("COMPANIA")]
        public string Compania { get; set; }

```

En `Modelo/GeoAgrLaborAuxiliarDet.cs` eliminar el mismo bloque (líneas 13-14).

En `LocalEntities.cs` eliminar `public string? Compania { get; set; }` de `GeoAgrLaborAuxiliarLocal` y de `GeoAgrLaborAuxiliarDetLocal`.

- [ ] **Step 4: DDL sin `Compania`**

En el constructor de `MarcasDBLocal`, reemplazar los dos `CREATE TABLE` de labor auxiliar por:

```csharp
                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrLaborAuxiliar (
                    IdRegistro  INTEGER NOT NULL,
                    IdEmpleado  TEXT    NOT NULL,
                    Fecha       TEXT    NOT NULL,
                    Comentarios TEXT,
                    EstadoId    INTEGER NOT NULL,
                    PRIMARY KEY (IdRegistro, IdEmpleado, Fecha)
                );").Wait();

                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrLaborAuxiliarDet (
                    IdRegistro          INTEGER NOT NULL,
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
                    PRIMARY KEY (IdRegistro, IdEmpleado, Fecha, Item, IdEmpleadoAsignado),
                    FOREIGN KEY (IdRegistro, IdEmpleado, Fecha)
                        REFERENCES GeoAgrLaborAuxiliar (IdRegistro, IdEmpleado, Fecha)
                        ON DELETE CASCADE
                );").Wait();
```

Conservar la sentencia `DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDetEmpAsig` que está entre ambas, y los comentarios de advertencia sobre los `DROP` comentados.

- [ ] **Step 5: Métodos de labor auxiliar sin `compania`**

En `MarcasDBLocal`, reemplazar desde `private static GeoAgrLaborAuxiliar MapLocalToAuxiliar` hasta el final de `DeleteGeoAgrLaborAuxiliarDetAsync(string compania, ...)` y `MapDetLocalToDet` por:

```csharp
        private static GeoAgrLaborAuxiliar MapLocalToAuxiliar(GeoAgrLaborAuxiliarLocal l) =>
            new GeoAgrLaborAuxiliar
            {
                IdRegistro  = l.IdRegistro,
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

        public async Task<List<GeoAgrLaborAuxiliar>> GetGeoAgrLaborAuxiliarAsync(string idEmpleado)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarLocal>()
                .Where(l => l.IdEmpleado == idEmpleado)
                .ToListAsync();
            return locals.Select(MapLocalToAuxiliar).ToList();
        }

        public async Task<GeoAgrLaborAuxiliar?> GetGeoAgrLaborAuxiliarAsync(string idEmpleado, DateTime fecha, long idRegistro)
        {
            var l = await database.Table<GeoAgrLaborAuxiliarLocal>()
                .FirstOrDefaultAsync(i => i.IdEmpleado == idEmpleado
                                       && i.Fecha == fecha
                                       && i.IdRegistro == idRegistro);
            return l is null ? null : MapLocalToAuxiliar(l);
        }

        public async Task<List<GeoAgrLaborAuxiliar>> GetGeoAgrLaborAuxiliarAsync(string idEmpleado, DateTime fechaInicio, DateTime fechaFinal)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarLocal>()
                .Where(i => i.IdEmpleado == idEmpleado
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
            var existingDict   = existingLocals.ToDictionary(l => (l.IdEmpleado!, l.Fecha));

            var toInsert = new List<GeoAgrLaborAuxiliarLocal>();
            var toUpdate = new List<GeoAgrLaborAuxiliarLocal>();

            // Próximo IdRegistro disponible (MAX+1 o 1 si la tabla está vacía)
            long nextId = existingLocals.Count > 0
                ? existingLocals.Max(l => l.IdRegistro) + 1
                : 1;

            foreach (var item in list)
            {
                if (existingDict.TryGetValue((item.IdEmpleado, item.Fecha), out var local))
                {
                    local.Comentarios = item.Comentarios;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new GeoAgrLaborAuxiliarLocal
                    {
                        IdRegistro  = nextId++,
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
                          WHERE IdRegistro=? AND IdEmpleado=? AND Fecha=?",
                        u.Comentarios, u.IdRegistro, u.IdEmpleado, u.Fecha);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        public async Task<int> SaveGeoAgrLaborAuxiliarEstadoAsync(long idRegistro)
        {
            // Marca el registro como "En Proceso de Autorización" (1), excepto los detalles ya
            // finalizados —2 (Aprobado) y 99 (Aplicado)—, que conservan su estado y no se reenvían.
            await database.ExecuteAsync(@"UPDATE GeoAgrLaborAuxiliar SET EstadoId=? WHERE IdRegistro=? AND EstadoId NOT IN (2, 99)", 1, idRegistro);
            await database.ExecuteAsync(@"UPDATE GeoAgrLaborAuxiliarDet SET EstadoId=? WHERE IdRegistro=? AND EstadoId NOT IN (2, 99)", 1, idRegistro);
            return 1;
        }

        public async Task<bool> DeleteGeoAgrLaborAuxiliarAsync()
        {
            await database.ExecuteAsync("DELETE FROM GeoAgrLaborAuxiliar");
            return true;
        }

        public async Task<bool> DeleteGeoAgrLaborAuxiliarAsync(string idEmpleado, DateTime fecha)
        {
            await database.ExecuteAsync(
                "DELETE FROM GeoAgrLaborAuxiliar WHERE IdEmpleado=? AND Fecha=?",
                idEmpleado, fecha);
            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // GeoAgrLaborAuxiliarDet
        // ════════════════════════════════════════════════════════════════════════

        public async Task<List<GeoAgrLaborAuxiliarDet>> GetGeoAgrLaborAuxiliarDetAsync()
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetLocal>().ToListAsync();
            return locals.Select(MapDetLocalToDet).ToList();
        }

        public async Task<List<GeoAgrLaborAuxiliarDet>> GetGeoAgrLaborAuxiliarDetAsync(string idEmpleado)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetLocal>()
                .Where(l => l.IdEmpleado == idEmpleado)
                .ToListAsync();
            return locals.Select(MapDetLocalToDet).ToList();
        }

        public async Task<List<GeoAgrLaborAuxiliarDet>> GetGeoAgrLaborAuxiliarDetAsync(string idEmpleado, DateTime fechaInicio, DateTime fechaFinal)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetLocal>()
                .Where(l => l.IdEmpleado == idEmpleado
                        && l.Fecha >= fechaInicio
                        && l.Fecha <= fechaFinal
                        && (l.EstadoId != 99 && l.EstadoId != 100))
                .ToListAsync();
            return locals.Select(MapDetLocalToDet).ToList();
        }

        public async Task<List<GeoAgrLaborAuxiliarDet>> GetGeoAgrLaborAuxiliarDetAsync(string idEmpleado, DateTime fecha, long idRegistro)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetLocal>()
                .Where(l => l.IdEmpleado == idEmpleado && l.Fecha == fecha && l.IdRegistro == idRegistro)
                .ToListAsync();
            return locals.Select(MapDetLocalToDet).ToList();
        }

        public async Task<GeoAgrLaborAuxiliarDet?> GetGeoAgrLaborAuxiliarDetPorItemAsync(string idEmpleado, DateTime fecha, int item)
        {
            var l = await database.Table<GeoAgrLaborAuxiliarDetLocal>()
                .FirstOrDefaultAsync(i => i.IdEmpleado == idEmpleado && i.Fecha == fecha && i.Item == item);
            if (l is null) return null;
            return MapDetLocalToDet(l);
        }

        public async Task<int> SaveGeoAgrLaborAuxiliarDetAsync(IEnumerable<GeoAgrLaborAuxiliarDet> detalles)
        {
            var list = detalles.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrLaborAuxiliarDetLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(l => (l.IdEmpleado!, l.Fecha, l.Item, l.IdEmpleadoAsignado!));

            var toInsert = new List<GeoAgrLaborAuxiliarDetLocal>();
            var toUpdate = new List<GeoAgrLaborAuxiliarDetLocal>();

            // Cache de IdRegistro del encabezado padre para evitar múltiples consultas
            var idRegistroCache = new Dictionary<(string, DateTime), long>();

            foreach (var item in list)
            {
                var idEmpAsig = item.IdEmpleadoAsignado ?? item.IdEmpleado;

                // Resolver IdRegistro del encabezado padre
                var cacheKey = (item.IdEmpleado, item.Fecha);
                if (!idRegistroCache.TryGetValue(cacheKey, out long idRegistro))
                {
                    var padre = await database.Table<GeoAgrLaborAuxiliarLocal>()
                        .FirstOrDefaultAsync(l => l.IdEmpleado == item.IdEmpleado
                                               && l.Fecha      == item.Fecha);
                    idRegistro = padre?.IdRegistro ?? 0;
                    idRegistroCache[cacheKey] = idRegistro;
                }

                if (existingDict.TryGetValue((item.IdEmpleado, item.Fecha, item.Item, idEmpAsig), out var local))
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
                        IdEmpleado           = item.IdEmpleado,
                        Fecha                = item.Fecha,
                        Item                 = item.Item,
                        Bloque               = item.Bloque,
                        Finca                = item.Finca,
                        Lote                 = item.Lote,
                        Ot                   = item.Ot,
                        Labor                = item.Labor,
                        Recurso              = item.Recurso,
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
                          SET IdRegistro=?, Bloque=?, Finca=?, Lote=?, Ot=?, Labor=?, Recurso=?,
                              CantidadRendimiento=?, Unidad=?, HorasAprobadas=?, EstadoId=?
                          WHERE IdEmpleado=? AND Fecha=? AND Item=? AND IdEmpleadoAsignado=?",
                        u.IdRegistro, u.Bloque, u.Finca, u.Lote, u.Ot, u.Labor, u.Recurso,
                        u.CantidadRendimiento, u.Unidad, u.HorasAprobadas, u.EstadoId,
                        u.IdEmpleado, u.Fecha, u.Item, u.IdEmpleadoAsignado);
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

        public async Task<bool> DeleteGeoAgrLaborAuxiliarDetAsync(string idEmpleado, DateTime fecha)
        {
            await database.ExecuteAsync(
                "DELETE FROM GeoAgrLaborAuxiliarDet WHERE IdEmpleado=? AND Fecha=?",
                idEmpleado, fecha);
            return true;
        }

        private static GeoAgrLaborAuxiliarDet MapDetLocalToDet(GeoAgrLaborAuxiliarDetLocal l) =>
            new GeoAgrLaborAuxiliarDet
            {
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
```

Nota: el `UPDATE` de detalle ahora incluye `Recurso=?`, que antes se omitía (el recurso no se actualizaba al hacer upsert).

En los métodos por `IdRegistro` (`InsertGeoAgrLaborAuxiliarAsync`, `SaveGeoAgrLaborAuxiliarDetAsync(long idRegistro, ...)`), eliminar la línea `Compania = labor.Compania,` y `Compania = item.Compania,` respectivamente.

- [ ] **Step 6: Consumidores — quitar `compania`**

`Principal.razor`:
- líneas 117-119: reemplazar `loginState.CompaniaSeleccionada` por `loginState.CompaniaTenant` en `GetSolicitudAuxLaborByResponsable` y quitarlo de `GetGeoAgrLaborAuxiliarAsync(...)` / `GetGeoAgrLaborAuxiliarDetAsync(...)`:

```csharp
            var solicitudAuxLaborTask = geoConnect.GetSolicitudAuxLaborByResponsable(loginState.CompaniaTenant, loginState.Empleado.IdNumero!, DateTime.Now.AddDays(-35).ToString("yyyyMMdd"), DateTime.Now.AddDays(1).ToString("yyyyMMdd"));
            var laborAuxiliarTask = marcaDBLocal.GetGeoAgrLaborAuxiliarAsync(loginState.Empleado.IdNumero!, DateTime.Now.AddDays(-35), DateTime.Now.AddDays(1));
            var itemsLaborAuxiliarTask = marcaDBLocal.GetGeoAgrLaborAuxiliarDetAsync(loginState.Empleado.IdNumero!, DateTime.Now.AddDays(-35), DateTime.Now.AddDays(1));
```
- línea 266 y 275: `COMPANIA = labor.Compania!` → `COMPANIA = loginState.CompaniaTenant`.
- línea 290: `SaveGeoAgrLaborAuxiliarEstadoAsync(labor.IdRegistro, labor.Compania!)` → `SaveGeoAgrLaborAuxiliarEstadoAsync(labor.IdRegistro)`.
- Antes del bloque "Enviar labores pendientes" agregar la guarda del tenant:

```csharp
            var laboresPendientes = laboresAuxiliares?.Where(l => l.EstadoId == 0).ToList();
            if (string.IsNullOrEmpty(loginState.CompaniaTenant) && laboresPendientes?.Count > 0)
            {
                await swal.FireAsync("Atención", "No se pudo determinar la compañía de la sesión; las labores pendientes no se enviaron.", SweetAlertIcon.Warning);
                laboresPendientes = null;
            }
```
(y quitar la declaración original `var laboresPendientes = ...` de la línea 245).

`SincronizacionManual.razor`: mismos cambios en líneas 135-137, 275, 284, 299, y la misma guarda (líneas 254-255).

`GeoAgrLaborAuxiliarList.razor`:
- línea 226: `GetGeoAgrLaborAuxiliarDetAsync(r.IdEmpleado!, r.Fecha, r.IdRegistro)`.
- línea 231: eliminar `Compania = r.Compania,` y la propiedad `Compania` de `GeoAgrLaborAuxiliarExt` si está declarada en este archivo.
- líneas 263-277: `loginState.CompaniaSeleccionada` → `loginState.CompaniaTenant` en `GetSolicitudAuxLaborByResponsable`, y quitar el primer argumento de las dos llamadas locales.
- línea 330: `NavigateTo($"./geoagrlaborauxiliaredicion?idEmpleado={Uri.EscapeDataString(labor.IdEmpleado ?? "")}&fecha={fecha}&idregistro={labor.IdRegistro}")`.
- Revisar el método `OnDelete` (a continuación de la línea 344) y quitar `compania` de las llamadas `DeleteGeoAgrLaborAuxiliar*`.

`GeoAgrLaborAuxiliarEdicion.razor`:
- eliminar `[SupplyParameterFromQuery] public string? compania { get; set; }` (línea 161).
- líneas 246-247 y 351/354: quitar `compania!,` de `GetGeoAgrLaborAuxiliarAsync` / `GetGeoAgrLaborAuxiliarDetAsync`.
- líneas 371 y 380: `COMPANIA = loginState.CompaniaTenant,`.
- línea 398: `SaveGeoAgrLaborAuxiliarEstadoAsync(idRegistroActual)`.
- al inicio de `GuardarEnGeotime` (después de `try {`):

```csharp
            if (string.IsNullOrEmpty(loginState.CompaniaTenant))
            {
                await swal.FireAsync("Atención", "No se pudo determinar la compañía de la sesión; el cambio quedó solo en el dispositivo.", SweetAlertIcon.Warning);
                return;
            }
```

`GeoAgrLaborAuxiliarDetalle.razor`:
- clase `EncabezadoForm` (líneas 314-320): eliminar `Compania`.
- línea 406: eliminar `encabezado.Compania = loginState.Empleado.IdCCosto;`.
- `AgregarDetalle` (línea 424): eliminar `Compania = loginState.CompaniaSeleccionada,`.
- `GuardarDatos`: eliminar `var comp = loginState.CompaniaSeleccionada;`, `Compania = comp,` en `header` y en cada detalle (líneas 448, 453, 466).
- `GuardarEnGeotime` (líneas 521, 530): `COMPANIA = loginState.CompaniaTenant,` y la misma guarda del tenant al inicio del `try`.
- línea 547: `SaveGeoAgrLaborAuxiliarEstadoAsync(idRegistroActual)`.

- [ ] **Step 7: Compilar**

Run: comando de compilación de Global Constraints.
Expected: `Build succeeded`. Los errores restantes de `CompaniaSeleccionada`/`PaisSeleccionado` quedan solo en `MainLayout.razor` (Task 6). Si el proyecto no compila por eso, aplicar primero el Step 1-4 de Task 6 y volver.

- [ ] **Step 8: Verificar la captura y el envío**

Con la app en ejecución: crear una labor con 2 colaboradores y 1 línea; guardarla.
Run: `sqlite3 "$env:LOCALAPPDATA\LaboresAGR\LaboresAGR.db3" "select IdRegistro,IdEmpleado,Fecha,EstadoId from GeoAgrLaborAuxiliar; select IdRegistro,Item,Bloque,Finca,Lote,Ot,Labor,Recurso,Unidad,IdEmpleadoAsignado,EstadoId from GeoAgrLaborAuxiliarDet;"`
Expected: 1 fila de encabezado y 2 de detalle; `EstadoId = 1` si la API recibió la solicitud. En Geotime, `SELECT COMPANIA FROM <tabla de solicitud> WHERE IDREGISTROAPP = <IdRegistro>` devuelve el `IDCOMP` del login.

- [ ] **Step 9: Commit**

```bash
git add LaboresNet
git commit -m "refactor: eliminar Compania de labores auxiliares; usar tenant de la sesión"
```

---

## Task 5: Migración desde la base anterior

**Files:**
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs` (constructor y un método privado nuevo)
- Modify: `LaboresNet/Data/DataBaseLocal/LocalEntities.cs` (clase nueva de lectura, ver Step 1)

**Interfaces:**
- Consumes: `NombreBaseLocal` (Task 2), `GeoAgrLaborAuxiliarLocal`, `GeoAgrLaborAuxiliarDetLocal`, `cParametroApiLocal` (`Id` = 1).
- Produces: nada público; efecto: la base `LaboresAGR` recibe la configuración de API y las labores no finalizadas de `AvancesMoDB\LaboresDatabase.db3`, una sola vez.

- [ ] **Step 1: Abrir `cParametroApiLocal` y eliminar los campos de Ticofrut**

Leer `LocalEntities.cs` alrededor de las líneas 300-330 para ver la definición exacta de `cParametroApiLocal`. Eliminar las propiedades `ApiAuxiliarUrl` y `ApiAuxiliarSchema` (líneas 328-329). Eliminar los `case "ApiAuxiliarUrl"` y `case "ApiAuxiliarSchema"` de `Modelo/Utils/ConfigFile.cs` (líneas 49-54) y los dos `RadzenTextBox` que enlazan `ApiAuxiliarUrl` / `ApiAuxiliarSchema` en `ConfiguracionApis.razor` (líneas ~47 y ~54, junto con su `RadzenLabel`/contenedor).

- [ ] **Step 2: Tabla de control**

En el constructor de `MarcasDBLocal`, después del `CREATE TABLE` de `GeoAgrLaborAuxiliarDet` y antes de `database.CreateTableAsync<FiltroUsuarioLocal>()`:

```csharp
                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS MigracionLabores (
                    Id    INTEGER NOT NULL,
                    Fecha TEXT    NOT NULL,
                    PRIMARY KEY (Id)
                );").Wait();

                MigrarDesdeBaseAnterior();
```

- [ ] **Step 3: Implementar `MigrarDesdeBaseAnterior`**

Agregar a `MarcasDBLocal` (usar `using SQLite;`, ya presente):

```csharp
        /// <summary>
        /// Copia, una sola vez, desde la base anterior (AvancesMoDB\LaboresDatabase.db3, solo lectura):
        /// la configuración de las APIs y las labores auxiliares no finalizadas (EstadoId distinto de 2 y 99).
        /// Cada paso es tolerante: la base anterior puede no existir o ser de otra aplicación.
        /// </summary>
        private void MigrarDesdeBaseAnterior()
        {
            try
            {
                var yaMigrado = database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MigracionLabores WHERE Id = 1").Result;
                if (yaMigrado > 0) return;

                string rutaAnterior = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AvancesMoDB", "LaboresDatabase.db3");

                if (File.Exists(rutaAnterior))
                {
                    try
                    {
                        using var anterior = new SQLiteConnection(rutaAnterior, SQLiteOpenFlags.ReadOnly);

                        bool Existe(string tabla) =>
                            anterior.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=?", tabla) > 0;

                        // 1. Configuración de las APIs (sin los campos de Ticofrut, que ya no existen en la clase).
                        if (Existe("cParametroApiLocal"))
                        {
                            var cfg = anterior.Query<cParametroApiLocal>("SELECT * FROM cParametroApiLocal WHERE Id = 1").FirstOrDefault();
                            var yaTiene = database.Table<cParametroApiLocal>().FirstOrDefaultAsync(p => p.Id == 1).Result;
                            if (cfg is not null && yaTiene is null)
                                database.InsertAsync(cfg).Wait();
                        }

                        // 2. Labores auxiliares no finalizadas y sus detalles.
                        if (Existe("GeoAgrLaborAuxiliar") && Existe("GeoAgrLaborAuxiliarDet"))
                        {
                            var encabezados = anterior.Query<GeoAgrLaborAuxiliarLocal>(
                                "SELECT * FROM GeoAgrLaborAuxiliar WHERE EstadoId NOT IN (2, 99)");
                            var detalles = anterior.Query<GeoAgrLaborAuxiliarDetLocal>(
                                @"SELECT d.* FROM GeoAgrLaborAuxiliarDet d
                                  JOIN GeoAgrLaborAuxiliar h ON h.IdRegistro = d.IdRegistro
                                  WHERE h.EstadoId NOT IN (2, 99)");

                            database.RunInTransactionAsync(db =>
                            {
                                foreach (var h in encabezados)
                                    db.Execute(
                                        "INSERT OR IGNORE INTO GeoAgrLaborAuxiliar (IdRegistro, IdEmpleado, Fecha, Comentarios, EstadoId) VALUES (?,?,?,?,?)",
                                        h.IdRegistro, h.IdEmpleado, h.Fecha, h.Comentarios, h.EstadoId);

                                foreach (var d in detalles)
                                    db.Execute(
                                        @"INSERT OR IGNORE INTO GeoAgrLaborAuxiliarDet
                                          (IdRegistro, IdEmpleado, Fecha, Item, Bloque, Finca, Lote, Ot, Labor, Recurso,
                                           CantidadRendimiento, Unidad, HorasAprobadas, EstadoId, IdEmpleadoAsignado)
                                          VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                                        d.IdRegistro, d.IdEmpleado, d.Fecha, d.Item, d.Bloque, d.Finca, d.Lote, d.Ot,
                                        d.Labor, d.Recurso, d.CantidadRendimiento, d.Unidad, d.HorasAprobadas,
                                        d.EstadoId, d.IdEmpleadoAsignado);
                            }).Wait();
                        }
                    }
                    catch (Exception e)
                    {
                        // Base anterior bloqueada, corrupta o con otro esquema: se continúa con tablas vacías.
                        Console.WriteLine($"MigrarDesdeBaseAnterior: {e.Message}");
                    }
                }

                database.ExecuteAsync(
                    "INSERT OR REPLACE INTO MigracionLabores (Id, Fecha) VALUES (1, ?)",
                    DateTime.Now.ToString("yyyyMMddHHmmss")).Wait();
            }
            catch (Exception e)
            {
                // Nunca debe impedir el arranque de la app.
                Console.WriteLine($"MigrarDesdeBaseAnterior (control): {e.Message}");
            }
        }
```

Notas de implementación:
- Si `cParametroApiLocal` tiene la PK con otro nombre que `Id`, ajustar `WHERE Id = 1` y el predicado `p.Id == 1` al nombre real (ver Step 1).
- `Fecha` se copia tal como está almacenada en la base anterior (sqlite-net guarda `DateTime` como ticks o texto según la versión; al leer con la misma entidad y escribir con `db.Execute` el valor se serializa igual que el resto de inserciones de esta base).

- [ ] **Step 4: Compilar**

Run: comando de compilación de Global Constraints.
Expected: `Build succeeded`.

- [ ] **Step 5: Verificar la migración con datos reales**

Preparación: copiar la base anterior a un respaldo (`Copy-Item "$env:LOCALAPPDATA\AvancesMoDB\LaboresDatabase.db3" "$env:TEMP\LaboresDatabase.db3.bak"`) y borrar `$env:LOCALAPPDATA\LaboresAGR` para forzar una base nueva.
Run: iniciar la app; no debe pedir configurar la API (carga directo a login).
Run: `sqlite3 "$env:LOCALAPPDATA\LaboresAGR\LaboresAGR.db3" "select Id,Fecha from MigracionLabores; select count(*) from GeoAgrLaborAuxiliar; select count(*) from GeoAgrLaborAuxiliarDet; select Id, ApiGeoTimeUrl from cParametroApiLocal;"`
Expected: 1 fila en `MigracionLabores`; los conteos igualan `select count(*) from GeoAgrLaborAuxiliar where EstadoId not in (2,99)` de la base anterior; la URL coincide con la anterior. Reiniciar la app: `MigracionLabores` no cambia de fecha y los conteos no se duplican.

- [ ] **Step 6: Verificar casos adversos**

1. Renombrar temporalmente `AvancesMoDB\LaboresDatabase.db3`, borrar `LaboresAGR`, iniciar: la app arranca y pide configurar la API; `MigracionLabores` tiene 1 fila.
2. Con la base anterior abierta en otro proceso (p. ej. DB Browser en modo escritura), borrar `LaboresAGR` e iniciar: la app arranca sin excepción.
Restaurar el archivo original al terminar.

- [ ] **Step 7: Commit**

```bash
git add LaboresNet
git commit -m "feat: migrar configuración y labores pendientes desde la base anterior"
```

---

## Task 6: Quitar selectores País/Compañía, `FiltroUsuario` y semáforo AV

**Files:**
- Modify: `LaboresNet/Components/Layout/MainLayout.razor`
- Modify: `LaboresNet/Components/Layout/ApiSemaforo.razor`
- Modify: `LaboresNet/Components/Pages/Home.razor:33, 151, 168-172`
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs` (región `FiltroUsuario`, constructor)
- Modify: `LaboresNet/Data/DataBaseLocal/LocalEntities.cs` (`FiltroUsuarioLocal`)

**Interfaces:**
- Consumes: nada nuevo.
- Produces: `ApiSemaforo` solo recibe `GeoTimeOk` y `Oscuro`.

- [ ] **Step 1: `ApiSemaforo.razor`**

Reemplazar el archivo completo:

```razor
@*
    Indicador de disponibilidad de la API de GeoTime (GT).
    Estado: null = verificando, true = en línea, false = sin conexión.
    Oscuro = true para fondos oscuros (footer), false para fondos claros (Home).
*@

<div class="api-status @(Oscuro ? "api-status-dark" : "api-status-light")">
    <span class="api-pill" title="@TituloEstado("GeoTime", GeoTimeOk)">
        <span class="api-dot @ClaseEstado(GeoTimeOk)"></span>GT
    </span>
</div>

@code {
    [Parameter] public bool? GeoTimeOk { get; set; }
    [Parameter] public bool Oscuro { get; set; } = true;

    private static string ClaseEstado(bool? ok) => ok switch
    {
        true  => "api-ok",
        false => "api-down",
        _     => "api-checking"
    };

    private static string TituloEstado(string api, bool? ok) => ok switch
    {
        true  => $"{api}: en línea",
        false => $"{api}: sin conexión (modo offline)",
        _     => $"{api}: verificando..."
    };
}
```

- [ ] **Step 2: `MainLayout.razor` — footer sin filtros**

Reemplazar el bloque `<RadzenFooter>` (líneas 29-52) por:

```razor
    <RadzenFooter>
        <div class="footer-content">
            <ApiSemaforo GeoTimeOk="@geoTimeOk" />
        </div>
    </RadzenFooter>
```

En `@code`: eliminar `ticoFrutOk`, `listaPaises`, `listaCompanias`, `todasLasFincas` (líneas 63, 67-71), `CargarFiltrosFooter`, `FiltrarCompaniasPorPais`, `OnPaisChange`, `OnCompaniaChange`, `GuardarFiltro` (líneas 92-163) y la llamada `await CargarFiltrosFooter();` (línea 89). En `OnAfterRenderAsync` reemplazar las líneas 171-183 por:

```csharp
                /* Validar disponibilidad de la API de Geotime (una sola vez) */
                geoTimeOk    = await geoConnect.ApiDisponible();
                Desconectado = geoTimeOk != true;
                StateHasChanged();
```

(eliminar `tareaTicoFrut` y su `ContinueWith`). Dejar `OnInitializedAsync` solo con las validaciones de `loginState.Empleado`.

- [ ] **Step 3: `Home.razor`**

Línea 33: `<ApiSemaforo GeoTimeOk="@geoTimeOk" Oscuro="true" />`. Eliminar la línea 151 (`bool? ticoFrutOk;`) y las líneas 168-172 (`_ = ticoFrutConnect.ApiDisponible().ContinueWith(...)`).

- [ ] **Step 4: Eliminar `FiltroUsuario`**

En `MarcasDBLocal.cs`: borrar la región `FiltroUsuario — último País y Compañía seleccionados por empleado` (`GetFiltroUsuarioAsync`, `SaveFiltroUsuarioAsync`) y la línea `database.CreateTableAsync<FiltroUsuarioLocal>().Wait();` del constructor. En `LocalEntities.cs`: borrar `FiltroUsuarioLocal`.

- [ ] **Step 5: Compilar**

Run: comando de compilación de Global Constraints.
Expected: `Build succeeded`. Todo `CompaniaSeleccionada`/`PaisSeleccionado` ya no debe existir:
Run: `Grep` de `CompaniaSeleccionada|PaisSeleccionado|FiltroUsuario|ticoFrutOk` en `LaboresNet` (solo `*.cs` y `*.razor`).
Expected: sin coincidencias.

- [ ] **Step 6: Verificar visualmente**

Ejecutar la app: en login y en el footer solo aparece el indicador `GT`; no hay selectores País/Compañía. Con la API apagada el punto `GT` se pone en rojo y la app sigue entrando en modo offline.

- [ ] **Step 7: Commit**

```bash
git add LaboresNet
git commit -m "refactor: eliminar selectores País/Compañía, FiltroUsuario y semáforo AV"
```

---

## Task 7: Eliminar Ticofrut

**Files:**
- Delete: `LaboresNet/Data/TicoFrutConnect.cs`, `LaboresNet/Data/Interfaces/ITicoFrutConnect.cs`
- Modify: `LaboresNet/MauiProgram.cs:35`
- Modify: `LaboresNet/Components/_Imports.razor:37, 47`
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs:7`
- Modify: `LaboresNet/LaboresNet.csproj:159-161`
- Delete (opcional, con confirmación del usuario): `LaboresNet/Dll/TicoFrutLibs.dll`, `LaboresNet/Dll/TicoFrutLibs.pdb`

**Interfaces:** ninguna nueva.

- [ ] **Step 1: Confirmar que ya no hay usos**

Run: `Grep` de `TicoFrutLibs|ITicoFrutConnect|ticoFrutConnect|TicoFrutConnect` en `LaboresNet` (`*.cs`, `*.razor`).
Expected: solo `TicoFrutConnect.cs`, `ITicoFrutConnect.cs`, `MauiProgram.cs`, `_Imports.razor`, `MarcasDBLocal.cs` (`using`). Si aparece otro archivo, corregirlo antes de continuar. En particular `GeoAgrLaborAuxiliarList.razor`/`Detalle.razor` podrían usar tipos `TicoFrutLibs.Models` para `GeoAgrLaborAuxiliar`: los modelos propios están en `LaboresNet/Modelo` (`PortalEmpleadoGeotime.Client.Modelo`), no deben cambiar.

- [ ] **Step 2: Borrar el código y las referencias**

```bash
git rm LaboresNet/Data/TicoFrutConnect.cs LaboresNet/Data/Interfaces/ITicoFrutConnect.cs
```

- `MauiProgram.cs:35`: eliminar `.AddScoped<ITicoFrutConnect, TicoFrutConnect>()` (y el `using` si queda sin uso).
- `_Imports.razor:37`: eliminar `@using TicoFrutLibs.Models`. `_Imports.razor:47`: eliminar `@inject ITicoFrutConnect ticoFrutConnect`.
- `MarcasDBLocal.cs:7`: eliminar `using TicoFrutLibs.Models;`.
- `LaboresNet.csproj:159-161`: eliminar el bloque

```xml
      <Reference Include="TicoFrutLibs">
        <HintPath>Dll\TicoFrutLibs.dll</HintPath>
      </Reference>
```
(incluida su etiqueta de cierre `</Reference>` si existe en las líneas siguientes).

- [ ] **Step 3: Compilar**

Run: comando de compilación de Global Constraints.
Expected: `Build succeeded`. Si falla por un tipo de `TicoFrutLibs` (p. ej. `EventResponse` o `ApiResponse`), ubicarlo con `Grep`: `EventResponse` también existe en `com.gsitcr.geotime.Models.Response` (GeotimeModelsLib), así que basta con el `using com.gsitcr.geotime.Models.Response;`.

- [ ] **Step 4: Quitar los binarios**

Preguntar al usuario si borrar `Dll\TicoFrutLibs.dll` y `Dll\TicoFrutLibs.pdb` (la compilación ya no los usa). Si acepta: `git rm LaboresNet/Dll/TicoFrutLibs.dll LaboresNet/Dll/TicoFrutLibs.pdb` y recompilar.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: eliminar API y librería de Ticofrut"
```

---

## Task 8: Verificación final

**Files:** ninguno (solo verificación; corregir lo que aparezca).

- [ ] **Step 1: Barrido de referencias**

Run: `Grep` (`LaboresNet`, `*.cs` y `*.razor`) de los patrones y verificar:

| Patrón | Esperado |
|---|---|
| `(?i)ticofrut` | sin coincidencias, salvo comentarios históricos en `MarcasDBLocal.cs` (borrarlos si quedan) |
| `\bPais\b` | sin coincidencias en lógica de negocio |
| `CompaniaSeleccionada\|PaisSeleccionado` | sin coincidencias |
| `\.Compania\b` | solo `login.Compania` (Home.razor), `cPh_*` del login y `companiaSeleccionada` del login/marcador |
| `COMPANIA\s*=` | solo `loginState.CompaniaTenant` en los 4 sitios de envío (`Principal`, `SincronizacionManual`, `Edicion`, `Detalle`) |

- [ ] **Step 2: Contraste con la base de Geotime**

Cuando el MCP `sqlserver` esté disponible, para cada tabla `Ph_Labores*` verificar columnas y tamaños contra los modelos (`BloqueId varchar(4)`, `OrdenTrabajoId varchar(50)`, `Descripcion varchar(50|100|200)`, `IdConcepto int`, `UnidadId varchar(4)`) y comparar `COUNT(*) WHERE Estado = 1` con los conteos del Task 3 Step 9. Si el MCP sigue sin conectar, anotarlo como verificación pendiente en el resumen final.

- [ ] **Step 3: Recorrido manual completo**

1. Borrar `%LOCALAPPDATA%\LaboresAGR`; iniciar la app (migra configuración y pendientes).
2. Login online → Principal sincroniza catálogos automáticamente.
3. Registro de Labores → Agregar → capturar línea completa → Guardar → verificar `EstadoId = 1`.
4. Editar la labor, cambiar cantidad asignada, guardar.
5. Apagar la API; entrar en modo offline; crear una labor (queda con `EstadoId = 0`); encender la API y sincronizar manualmente → la labor pendiente pasa a `EstadoId = 1`.
6. Con la API apagada, ejecutar "Sincronizar Datos": aviso de falta de comunicación, sin cambios en catálogos.

- [ ] **Step 4: Actualizar el resumen para el usuario**

Dejar constancia, en el mensaje final, de:
- Las solicitudes enviadas antes del cambio usan el código de compañía de Ticofrut y no se devolverán en `GetSolicitudAuxLaborByResponsable` con la compañía del tenant (Review Focus 2).
- Los datos que ya no existen: `TipoSuelo`, `Variedad`, `TipoFactorMo`, `FactorMo`, `ConceptoMo`.
- Las verificaciones que quedaron pendientes (MCP `sqlserver`, pruebas con datos reales).

- [ ] **Step 5: Commit final (si hubo correcciones)**

```bash
git add -A
git commit -m "fix: ajustes tras verificación final de catálogos Geotime"
```
