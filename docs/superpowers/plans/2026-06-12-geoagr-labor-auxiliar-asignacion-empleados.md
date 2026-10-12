# Asignación de cantidad por empleado en Labor Auxiliar - Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor "Labor Auxiliar" so each local `GeoAgrLaborAuxiliarDet` row represents one (ítem × empleado asignado) assignment, drop `GeoAgrLaborAuxiliarDetEmpAsig` entirely, and add a new `GeoAgrLaborAuxiliarEdicion.razor` page that lets the user adjust each employee's assigned quantity independently.

**Architecture:** Add `IdEmpleadoAsignado` to `GeoAgrLaborAuxiliarDet` / `GeoAgrLaborAuxiliarDetLocal`, change the SQLite PK to `(IdRegistro, Compania, IdEmpleado, Fecha, Item, IdEmpleadoAsignado)`, drop `GeoAgrLaborAuxiliarDetEmpAsig` (table + DTO/local classes + all CRUD methods), add a migration `DROP TABLE` for both affected tables, update the upsert/insert helpers in `MarcasDBLocal.cs`, and add `UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync`. Simplify `GeoAgrLaborAuxiliarDetalle.razor` to "solo alta" (cartesian item × empleado expansion on save), add `GeoAgrLaborAuxiliarEdicion.razor` for per-employee quantity editing, and update `GeoAgrLaborAuxiliarList.razor` (counts + edit route) and `SincronizacionManual.razor` (status-update filter).

**Tech Stack:** .NET MAUI Blazor Hybrid (`net10.0-windows10.0.19041.0`), SQLite via `sqlite-net-pcl`, Radzen Blazor components, SweetAlert2.

**Spec:** `docs/superpowers/specs/2026-06-12-geoagr-labor-auxiliar-asignacion-empleados-design.md`

---

## Task 1: Modelo — agregar `IdEmpleadoAsignado` a `GeoAgrLaborAuxiliarDet`

**Files:**
- Modify: `LaboresNet/Modelo/GeoAgrLaborAuxiliarDet.cs:52-55`

- [ ] **Step 1: Agregar la propiedad `IdEmpleadoAsignado`**

In `LaboresNet/Modelo/GeoAgrLaborAuxiliarDet.cs`, replace:

```csharp
        [Column("ESTADOID")]
        public int EstadoId { get; set; } = 0;

        public long IdRegistro { get; set; } = 0;
```

with:

```csharp
        [Column("ESTADOID")]
        public int EstadoId { get; set; } = 0;

        /// <summary>Empleado al que se asignó este ítem (uno por cada empleado seleccionado al crear la labor). Sin columna en Oracle.</summary>
        public string? IdEmpleadoAsignado { get; set; }

        public long IdRegistro { get; set; } = 0;
```

- [ ] **Step 2: Commit**

```bash
git add LaboresNet/Modelo/GeoAgrLaborAuxiliarDet.cs
git commit -m "feat: agregar IdEmpleadoAsignado a GeoAgrLaborAuxiliarDet"
```

---

## Task 2: LocalEntities — agregar `IdEmpleadoAsignado` y eliminar `GeoAgrLaborAuxiliarDetEmpAsig`/`...Local`

**Files:**
- Modify: `LaboresNet/Data/DataBaseLocal/LocalEntities.cs:403-445`

- [ ] **Step 1: Agregar `IdEmpleadoAsignado` a `GeoAgrLaborAuxiliarDetLocal` y eliminar ambas clases `GeoAgrLaborAuxiliarDetEmpAsig`**

In `LaboresNet/Data/DataBaseLocal/LocalEntities.cs`, replace:

```csharp
    [Table("GeoAgrLaborAuxiliarDet")]
    public class GeoAgrLaborAuxiliarDetLocal
    {
        /// <summary>FK → GeoAgrLaborAuxiliar.IdRegistro; parte de la PK compuesta.</summary>
        public long      IdRegistro           { get; set; }
        public string?   Compania             { get; set; }
        public string?   IdEmpleado           { get; set; }
        public DateTime  Fecha                { get; set; }
        public int       Item                 { get; set; }
        public string?   Bloque               { get; set; }
        public string?   Finca                { get; set; }
        public string?   Lote                 { get; set; }
        public string?   Ot                   { get; set; }
        public string?   Labor                { get; set; }
        public string?  Recurso               { get; set; }
        public decimal?  CantidadRendimiento { get; set; }
        public string?   Unidad               { get; set; }
        public decimal?  HorasAprobadas       { get; set; }
        public int EstadoId { get; set; } = 0;
    }

    /// <summary>Modelo DTO para GeoAgrLaborAuxiliarDetEmpAsig (sin dependencia de TicoFrutLibs).</summary>
    public class GeoAgrLaborAuxiliarDetEmpAsig
    {
        public long      IdRegistro         { get; set; }
        public string?   Compania           { get; set; }
        public string?   IdEmpleado         { get; set; }
        public DateTime  Fecha              { get; set; }
        public string?   IdEmpleadoAsignado { get; set; }
        public int? EstadoId { get; set; }
    }

    [Table("GeoAgrLaborAuxiliarDetEmpAsig")]
    public class GeoAgrLaborAuxiliarDetEmpAsigLocal
    {
        /// <summary>FK → GeoAgrLaborAuxiliar.IdRegistro; parte de la PK compuesta.</summary>
        public long      IdRegistro         { get; set; }
        public string?   Compania           { get; set; }
        public string?   IdEmpleado         { get; set; }
        public DateTime  Fecha              { get; set; }
        public string?   IdEmpleadoAsignado { get; set; }
        public int? EstadoId { get; set; }
    }
```

with:

```csharp
    [Table("GeoAgrLaborAuxiliarDet")]
    public class GeoAgrLaborAuxiliarDetLocal
    {
        /// <summary>FK → GeoAgrLaborAuxiliar.IdRegistro; parte de la PK compuesta.</summary>
        public long      IdRegistro           { get; set; }
        public string?   Compania             { get; set; }
        public string?   IdEmpleado           { get; set; }
        public DateTime  Fecha                { get; set; }
        public int       Item                 { get; set; }
        public string?   Bloque               { get; set; }
        public string?   Finca                { get; set; }
        public string?   Lote                 { get; set; }
        public string?   Ot                   { get; set; }
        public string?   Labor                { get; set; }
        public string?  Recurso               { get; set; }
        public decimal?  CantidadRendimiento { get; set; }
        public string?   Unidad               { get; set; }
        public decimal?  HorasAprobadas       { get; set; }
        public int EstadoId { get; set; } = 0;
        /// <summary>Empleado al que se asignó este ítem; parte de la PK compuesta.</summary>
        public string?   IdEmpleadoAsignado   { get; set; }
    }
```

- [ ] **Step 2: Commit**

```bash
git add LaboresNet/Data/DataBaseLocal/LocalEntities.cs
git commit -m "feat: agregar IdEmpleadoAsignado a GeoAgrLaborAuxiliarDetLocal y eliminar GeoAgrLaborAuxiliarDetEmpAsig"
```

---

## Task 3: Esquema SQLite — nueva PK de `GeoAgrLaborAuxiliarDet`, eliminar tabla `GeoAgrLaborAuxiliarDetEmpAsig`, migración

**Files:**
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs:122-172`

- [ ] **Step 1: Agregar los `DROP TABLE` de migración**

In `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs`, replace:

```csharp
                // Migración: recrear tablas con IdRegistro como parte de la PK compuesta.
                // Se eliminan las tablas previas para aplicar el nuevo esquema.
                //database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDet").Wait();
                //database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliar").Wait();
```

with:

```csharp
                // Migración: recrear tablas con IdRegistro como parte de la PK compuesta.
                // Se eliminan las tablas previas para aplicar el nuevo esquema.
                //database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDet").Wait();
                //database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliar").Wait();

                // Migración: GeoAgrLaborAuxiliarDet ahora incluye IdEmpleadoAsignado en su PK
                // y GeoAgrLaborAuxiliarDetEmpAsig se elimina por completo.
                database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDet").Wait();
                database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDetEmpAsig").Wait();
```

- [ ] **Step 2: Reescribir la tabla `GeoAgrLaborAuxiliarDet` con la nueva PK y eliminar la tabla `GeoAgrLaborAuxiliarDetEmpAsig`**

In `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs`, replace:

```csharp
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
                    PRIMARY KEY (IdRegistro, Compania, IdEmpleado, Fecha, Item),
                    FOREIGN KEY (IdRegistro, Compania, IdEmpleado, Fecha)
                        REFERENCES GeoAgrLaborAuxiliar (IdRegistro, Compania, IdEmpleado, Fecha)
                        ON DELETE CASCADE
                );").Wait();


                database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS GeoAgrLaborAuxiliarDetEmpAsig (
                    IdRegistro          INTEGER NOT NULL,
                    Compania            TEXT    NOT NULL,
                    IdEmpleado          TEXT    NOT NULL,
                    Fecha               TEXT    NOT NULL,                    
                    IdempleadoAsignado  TEXT    NOT NULL,
                    PRIMARY KEY (IdRegistro, Compania, IdEmpleado, Fecha, IdempleadoAsignado),
                    FOREIGN KEY (IdRegistro, Compania, IdEmpleado, Fecha)
                        REFERENCES GeoAgrLaborAuxiliar (IdRegistro, Compania, IdEmpleado, Fecha)
                        ON DELETE CASCADE
                );").Wait();

                database.CreateTableAsync<FiltroUsuarioLocal>().Wait();
```

with:

```csharp
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
```

> **Nota para el implementador:** si el `old_string` de este Step no calza exactamente (espacios/tabs), use `Read` sobre `MarcasDBLocal.cs:122-172` para obtener el texto exacto y ajuste el `old_string` en consecuencia — el `new_string` de arriba es el resultado final correcto.

- [ ] **Step 3: Commit**

```bash
git add LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs
git commit -m "feat: nueva PK de GeoAgrLaborAuxiliarDet con IdEmpleadoAsignado y eliminar tabla GeoAgrLaborAuxiliarDetEmpAsig"
```

---

## Task 4: MarcasDBLocal — actualizar upsert de `GeoAgrLaborAuxiliarDet` con `IdEmpleadoAsignado`

**Files:**
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs:3330-3444`

- [ ] **Step 1: Agregar `IdEmpleadoAsignado` al mapeo `MapDetLocalToDet`**

In `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs`, replace:

```csharp
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
            };
```

with:

```csharp
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
```

- [ ] **Step 2: Reescribir el upsert `SaveGeoAgrLaborAuxiliarDetAsync(IEnumerable<GeoAgrLaborAuxiliarDet>)` con clave `IdEmpleadoAsignado`**

In `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs`, replace:

```csharp
        public async Task<int> SaveGeoAgrLaborAuxiliarDetAsync(IEnumerable<GeoAgrLaborAuxiliarDet> detalles)
        {
            var list = detalles.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrLaborAuxiliarDetLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(l => (l.Compania!, l.IdEmpleado!, l.Fecha, l.Item));

            var toInsert = new List<GeoAgrLaborAuxiliarDetLocal>();
            var toUpdate = new List<GeoAgrLaborAuxiliarDetLocal>();

            // Cache de IdRegistro del encabezado padre para evitar múltiples consultas
            var idRegistroCache = new Dictionary<(string, string, DateTime), long>();

            foreach (var item in list)
            {
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

                if (existingDict.TryGetValue((item.Compania, item.IdEmpleado, item.Fecha, item.Item), out var local))
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
                          WHERE Compania=? AND IdEmpleado=? AND Fecha=? AND Item=?",
                        u.IdRegistro, u.Bloque, u.Finca, u.Lote, u.Ot, u.Labor,
                        u.CantidadRendimiento, u.Unidad, u.HorasAprobadas, u.EstadoId,
                        u.Compania, u.IdEmpleado, u.Fecha, u.Item);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }
```

with:

```csharp
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
```

> **Nota para el implementador:** la línea `u.CantidadRendimiento, u.Unidad, u.HorasAprobadas, u.EstadoId,` en el archivo real puede tener espacios finales adicionales. Si el `old_string` no calza exactamente, use `Read` sobre `MarcasDBLocal.cs:3330-3410` para obtener el texto exacto y ajuste el `old_string` en consecuencia — el `new_string` de arriba es el resultado final correcto.

- [ ] **Step 3: Commit**

```bash
git add LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs
git commit -m "feat: actualizar upsert de GeoAgrLaborAuxiliarDet con clave IdEmpleadoAsignado"
```

---

## Task 5: MarcasDBLocal — incluir `IdEmpleadoAsignado` en el alta `SaveGeoAgrLaborAuxiliarDetAsync(long idRegistro, ...)`

**Files:**
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs:3694-3725`

- [ ] **Step 1: Agregar `IdEmpleadoAsignado` al `toInsert` y quitar la línea en blanco sobrante**

In `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs`, replace:

```csharp
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
            }).ToList();

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                totalAffected = db.InsertAll(toInsert);
            });
            return totalAffected;
        }
```

with:

```csharp
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
```

> **Nota para el implementador:** la línea en blanco entre `Unidad` y `HorasAprobadas` en el archivo real puede contener espacios. Si el `old_string` no calza exactamente, use `Read` sobre `MarcasDBLocal.cs:3694-3725` para obtener el texto exacto y ajuste el `old_string` en consecuencia — el `new_string` de arriba es el resultado final correcto.

- [ ] **Step 2: Commit**

```bash
git add LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs
git commit -m "feat: incluir IdEmpleadoAsignado en alta de GeoAgrLaborAuxiliarDet"
```

---

## Task 6: MarcasDBLocal — eliminar `GeoAgrLaborAuxiliarDetEmpAsig` y agregar `UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync`

**Files:**
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs:3482-3629` (eliminar sección completa)
- Modify: `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs:3727-3763` (reemplazar sección)

- [ ] **Step 1: Eliminar la sección completa "TicoFrut — GeoAgrLaborAuxiliarDetEmpAsig"**

In `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs`, replace:

```csharp
        // ════════════════════════════════════════════════════════════════════════
        // TicoFrut — GeoAgrLaborAuxiliarDetEmpAsig
        // ════════════════════════════════════════════════════════════════════════

        private static GeoAgrLaborAuxiliarDetEmpAsig MapLocalToDetEmpAsig(GeoAgrLaborAuxiliarDetEmpAsigLocal l) =>
            new GeoAgrLaborAuxiliarDetEmpAsig
            {
                IdRegistro         = l.IdRegistro,
                Compania           = l.Compania,
                IdEmpleado         = l.IdEmpleado,
                Fecha              = l.Fecha,
                IdEmpleadoAsignado = l.IdEmpleadoAsignado,
                EstadoId = l.EstadoId,
            };

        /// <summary>Devuelve todos los registros de empleados asignados.</summary>
        public async Task<List<GeoAgrLaborAuxiliarDetEmpAsig>> GetGeoAgrLaborAuxiliarDetEmpAsigAsync()
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetEmpAsigLocal>().ToListAsync();
            return locals.Select(MapLocalToDetEmpAsig).ToList();
        }

        /// <summary>Devuelve los empleados asignados filtrados por compañía y empleado responsable.</summary>
        public async Task<List<GeoAgrLaborAuxiliarDetEmpAsig>> GetGeoAgrLaborAuxiliarDetEmpAsigAsync(
            string compania, string idEmpleado)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetEmpAsigLocal>()
                .Where(l => l.Compania == compania && l.IdEmpleado == idEmpleado)
                .ToListAsync();
            return locals.Select(MapLocalToDetEmpAsig).ToList();
        }

        /// <summary>Devuelve los empleados asignados para una labor específica (compañía, empleado, fecha, IdRegistro).</summary>
        public async Task<List<GeoAgrLaborAuxiliarDetEmpAsig>> GetGeoAgrLaborAuxiliarDetEmpAsigAsync(
            string compania, string idEmpleado, DateTime fecha, long IdRegistro)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetEmpAsigLocal>()
                .Where(l => l.Compania == compania && l.IdEmpleado == idEmpleado && l.Fecha == fecha && l.IdRegistro == IdRegistro)
                .ToListAsync();
            return locals.Select(MapLocalToDetEmpAsig).ToList();
        }

        public async Task<List<GeoAgrLaborAuxiliarDetEmpAsig>> GetGeoAgrLaborAuxiliarDetEmpAsigAsync(
            string compania, string idEmpleado, DateTime fechaInicio, DateTime fechaFinal)
        {
            var locals = await database.Table<GeoAgrLaborAuxiliarDetEmpAsigLocal>()
                .Where(l => l.Compania == compania && l.IdEmpleado == idEmpleado && l.Fecha >= fechaInicio && l.Fecha <= fechaFinal)
                .ToListAsync();
            return locals.Select(MapLocalToDetEmpAsig).ToList();
        }

        /// <summary>
        /// Inserta o actualiza la lista de empleados asignados.
        /// Resuelve automáticamente el IdRegistro del encabezado padre.
        /// </summary>
        public async Task<int> SaveGeoAgrLaborAuxiliarDetEmpAsigAsync(
            IEnumerable<GeoAgrLaborAuxiliarDetEmpAsig> asignaciones)
        {
            var list = asignaciones.ToList();
            if (list.Count == 0) return 0;

            var existingLocals = await database.Table<GeoAgrLaborAuxiliarDetEmpAsigLocal>().ToListAsync();
            var existingDict   = existingLocals.ToDictionary(
                l => (l.Compania!, l.IdEmpleado!, l.Fecha, l.IdEmpleadoAsignado!));

            var toInsert = new List<GeoAgrLaborAuxiliarDetEmpAsigLocal>();
            var toUpdate = new List<GeoAgrLaborAuxiliarDetEmpAsigLocal>();

            // Cache de IdRegistro del encabezado padre para evitar múltiples consultas
            var idRegistroCache = new Dictionary<(string, string, DateTime), long>();

            foreach (var item in list)
            {
                // Resolver IdRegistro del encabezado padre
                var cacheKey = (item.Compania!, item.IdEmpleado!, item.Fecha);
                if (!idRegistroCache.TryGetValue(cacheKey, out long idRegistro))
                {
                    var padre = await database.Table<GeoAgrLaborAuxiliarLocal>()
                        .FirstOrDefaultAsync(l => l.Compania   == item.Compania
                                               && l.IdEmpleado == item.IdEmpleado
                                               && l.Fecha      == item.Fecha);
                    idRegistro = padre?.IdRegistro ?? 0;
                    idRegistroCache[cacheKey] = idRegistro;
                }

                var key = (item.Compania!, item.IdEmpleado!, item.Fecha, item.IdEmpleadoAsignado!);
                if (existingDict.TryGetValue(key, out var local))
                {
                    // La PK es la propia clave; solo actualizamos el IdRegistro si cambió
                    local.IdRegistro = idRegistro;
                    toUpdate.Add(local);
                }
                else
                {
                    toInsert.Add(new GeoAgrLaborAuxiliarDetEmpAsigLocal
                    {
                        IdRegistro         = idRegistro,
                        Compania           = item.Compania,
                        IdEmpleado         = item.IdEmpleado,
                        Fecha              = item.Fecha,
                        IdEmpleadoAsignado = item.IdEmpleadoAsignado,
                    });
                }
            }

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                foreach (var u in toUpdate)
                    db.Execute(
                        @"UPDATE GeoAgrLaborAuxiliarDetEmpAsig
                          SET IdRegistro=?
                          WHERE Compania=? AND IdEmpleado=? AND Fecha=? AND IdempleadoAsignado=?",
                        u.IdRegistro, u.Compania, u.IdEmpleado, u.Fecha, u.IdEmpleadoAsignado);
                if (toInsert.Count > 0)
                    totalAffected += db.InsertAll(toInsert);
            });
            return totalAffected;
        }

        /// <summary>Elimina todos los registros de empleados asignados.</summary>
        public async Task<bool> DeleteGeoAgrLaborAuxiliarDetEmpAsigAsync()
        {
            await database.ExecuteAsync("DELETE FROM GeoAgrLaborAuxiliarDetEmpAsig");
            return true;
        }

        /// <summary>Elimina los empleados asignados de una labor específica (compañía, empleado, fecha).</summary>
        public async Task<bool> DeleteGeoAgrLaborAuxiliarDetEmpAsigAsync(
            string compania, string idEmpleado, DateTime fecha)
        {
            await database.ExecuteAsync(
                "DELETE FROM GeoAgrLaborAuxiliarDetEmpAsig WHERE Compania=? AND IdEmpleado=? AND Fecha=?",
                compania, idEmpleado, fecha);
            return true;
        }

        /// <summary>Elimina un empleado asignado específico de una labor.</summary>
        public async Task<bool> DeleteGeoAgrLaborAuxiliarDetEmpAsigAsync(
            string compania, string idEmpleado, DateTime fecha, string idEmpleadoAsignado)
        {
            await database.ExecuteAsync(
                @"DELETE FROM GeoAgrLaborAuxiliarDetEmpAsig
                  WHERE Compania=? AND IdEmpleado=? AND Fecha=? AND IdempleadoAsignado=?",
                compania, idEmpleado, fecha, idEmpleadoAsignado);
            return true;
        }

```

with: *(nada — eliminar el bloque completo, dejando una sola línea en blanco entre el `}` anterior y el siguiente encabezado de sección `// TicoFrut — GeoAgrLaborAuxiliar  (métodos por IdRegistro)`)*

> **Nota para el implementador:** este bloque es grande (148 líneas). Antes de aplicar el `old_string`, use `Read` sobre `MarcasDBLocal.cs:3480-3632` para confirmar que el texto coincide exactamente (incluyendo la línea en blanco final antes del siguiente encabezado de sección). Si no coincide, ajuste el `old_string` con el texto real — el resultado final debe ser: el `}` de la línea 3480 seguido de una línea en blanco y luego directamente el encabezado `// TicoFrut — GeoAgrLaborAuxiliar  (métodos por IdRegistro)`.

- [ ] **Step 2: Reemplazar la sección "EmpAsig por IdRegistro" con `UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync`**

In `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs`, replace:

```csharp
        // ── EmpAsig por IdRegistro ───────────────────────────────────────────────

        /// <summary>Elimina todos los empleados asignados de una labor identificada por IdRegistro.</summary>
        public async Task<bool> DeleteGeoAgrLaborAuxiliarDetEmpAsigAsync(long idRegistro)
        {
            await database.ExecuteAsync(
                "DELETE FROM GeoAgrLaborAuxiliarDetEmpAsig WHERE IdRegistro=?",
                idRegistro);
            return true;
        }

        /// <summary>
        /// Inserta (sin upsert) todos los empleados asignados asociando el IdRegistro dado.
        /// Se llama tras borrar los anteriores con DeleteGeoAgrLaborAuxiliarDetEmpAsigAsync(idRegistro).
        /// </summary>
        public async Task<int> SaveGeoAgrLaborAuxiliarDetEmpAsigAsync(
            long idRegistro, IEnumerable<GeoAgrLaborAuxiliarDetEmpAsig> asignaciones)
        {
            var list = asignaciones.ToList();
            if (list.Count == 0) return 0;

            var toInsert = list.Select(item => new GeoAgrLaborAuxiliarDetEmpAsigLocal
            {
                IdRegistro         = idRegistro,
                Compania           = item.Compania,
                IdEmpleado         = item.IdEmpleado,
                Fecha              = item.Fecha,
                IdEmpleadoAsignado = item.IdEmpleadoAsignado,
            }).ToList();

            int totalAffected = 0;
            await database.RunInTransactionAsync(db =>
            {
                totalAffected = db.InsertAll(toInsert);
            });
            return totalAffected;
        }
```

with:

```csharp
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
```

- [ ] **Step 3: Commit**

```bash
git add LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs
git commit -m "feat: eliminar GeoAgrLaborAuxiliarDetEmpAsig y agregar UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync"
```

---

## Task 7: Checkpoint — build intermedio

**Files:** (ninguno — solo verificación)

- [ ] **Step 1: Ejecutar el build**

Run: `dotnet build LaboresNet/LaboresNet.csproj -f net10.0-windows10.0.19041.0`

- [ ] **Step 2: Verificar los errores esperados**

En este punto del plan, `GeoAgrLaborAuxiliarDetEmpAsig`/`GeoAgrLaborAuxiliarDetEmpAsigLocal` y sus métodos CRUD ya no existen, pero tres archivos `.razor` todavía los referencian. El build debe fallar **únicamente** con errores en estos tres archivos:

- `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor` (referencias a `listaEmpAsig`, `GeoAgrLaborAuxiliarDetEmpAsig`, `SaveGeoAgrLaborAuxiliarDetEmpAsigAsync`, `GetGeoAgrLaborAuxiliarDetEmpAsigAsync`, `DeleteGeoAgrLaborAuxiliarDetEmpAsigAsync`)
- `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor` (referencia a `GetGeoAgrLaborAuxiliarDetEmpAsigAsync`)
- `LaboresNet/Components/Pages/Procesos/SincronizacionManual.razor` (referencias a `itemsLaborAuxiliarEmpTask`, `itemsLaborAuxiliarEmp`, `GetGeoAgrLaborAuxiliarDetEmpAsigAsync`, `SaveGeoAgrLaborAuxiliarDetEmpAsigAsync`)

Si aparecen errores en **cualquier otro archivo**, deténgase e investigue antes de continuar — significa que algo en las Tareas 1-6 quedó inconsistente. Las Tareas 8-12 resuelven los errores de estos tres archivos.

No hay commit para esta tarea (es solo un checkpoint de verificación).

---

## Task 8: GeoAgrLaborAuxiliarDetalle.razor — simplificar a modo "solo alta"

**Files:**
- Modify: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`

- [ ] **Step 1: Eliminar el bloque de "Registro No." / badge de Estado (modo edición) del encabezado**

In `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`, replace:

```razor
                    <div style="display:flex;flex-direction:column;gap:8px;">
                         @if (!esNuevo)
                        {
                            <label class="lbl-xs" style="display:flex; align-items:end; gap:10px;">

                                <div style="display:flex; flex-direction:column; gap:3px;">
                                    <span>Registro No.</span>
                                    <InputNumber class="form-control br8"
                                                 readonly="true"
                                                 @bind-Value="@idRegistroActual" />
                                </div>

                                <span class="badge bg-primary">
                                    @encabezado.Estado
                                </span>

                            </label>
                        }
                        <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                            Fecha
```

with:

```razor
                    <div style="display:flex;flex-direction:column;gap:8px;">
                        <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                            Fecha
```

- [ ] **Step 2: Eliminar los parámetros de query y el campo `esNuevo`**

In `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`, replace:

```csharp
    [SupplyParameterFromQuery] public string? compania    { get; set; }
    [SupplyParameterFromQuery] public string? idEmpleado  { get; set; }
    [SupplyParameterFromQuery] public string? fecha       { get; set; }
    [SupplyParameterFromQuery] public long?   idregistro  { get; set; }

    // IdRegistro del registro actual (0 mientras es nuevo, asignado tras el primer Insert)
    private long idRegistroActual = 0;

    private IEnumerable<EmpleadoExt> empleados = Enumerable.Empty<EmpleadoExt>();
    RadzenDataGrid<EmpleadoExt>? gridEmpleados;
    List<EmpleadoExt> empleadosSeleccionados = new();

    private string Titulo    = "Labor Auxiliar";
    private string UrlRetorno = "./geoagrlaborauxiliarlist";
    bool InProcess = true;
    bool esNuevo   = true;
    bool sincronizar = true;
```

with:

```csharp
    // IdRegistro asignado al insertar el encabezado nuevo
    private long idRegistroActual = 0;

    private IEnumerable<EmpleadoExt> empleados = Enumerable.Empty<EmpleadoExt>();
    RadzenDataGrid<EmpleadoExt>? gridEmpleados;
    List<EmpleadoExt> empleadosSeleccionados = new();

    private string Titulo    = "Labor Auxiliar";
    private string UrlRetorno = "./geoagrlaborauxiliarlist";
    bool InProcess = true;
    bool sincronizar = true;
```

- [ ] **Step 3: Eliminar la propiedad `Estado` de `EncabezadoForm`**

In `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`, replace:

```csharp
    public class EncabezadoForm
    {
        public string?  Compania    { get; set; }
        public string?  IdEmpleado  { get; set; }
        public DateOnly FechaDO     { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public string?  Comentarios { get; set; }
        public string? Estado { get; set; } = "";
    }
```

with:

```csharp
    public class EncabezadoForm
    {
        public string?  Compania    { get; set; }
        public string?  IdEmpleado  { get; set; }
        public DateOnly FechaDO     { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public string?  Comentarios { get; set; }
    }
```

- [ ] **Step 4: Reescribir `OnInitializedAsync` como solo-alta**

In `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`, replace:

```csharp
    protected override async Task OnInitializedAsync()
    {
        try
        {
            // ── Catálogos: independientes entre sí → se lanzan sin await ─────────
            var tFincas    = marcaDBLocal.GetGeoAgrFincaAsync(loginState.CompaniaSeleccionada, loginState.PaisSeleccionado);
            var tLotes     = marcaDBLocal.GetGeoAgrLoteAsync(loginState.CompaniaSeleccionada);
            var tOts       = marcaDBLocal.GetGeoAgrOtAsync(loginState.CompaniaSeleccionada);
            var tOtLabores = marcaDBLocal.GetGeoAgrOtLaborAsync(loginState.CompaniaSeleccionada);
            var tRecursoConcepto = marcaDBLocal.GetGeoAgrRecursoConceptoAsync(loginState.CompaniaSeleccionada);

            // Fecha de la labor: si viene por URL (edición) se usa esa; si no, hoy (nuevo)
            var fechaLabor = !string.IsNullOrEmpty(fecha)
                ? DateTime.ParseExact(fecha, "yyyyMMdd", null)
                : DateTime.Today;

            var empleadosTask = CargarEmpleados(fechaLabor);

            if (!string.IsNullOrEmpty(fecha))
            {
                // Modo edición: todas las consultas son independientes → paralelo total
                esNuevo = false;
                idRegistroActual = idregistro ?? 0;
                var fechaDT = DateTime.ParseExact(fecha, "yyyyMMdd", null);

                encabezado.Compania   = compania;
                encabezado.IdEmpleado = idEmpleado;
                encabezado.FechaDO    = DateOnly.FromDateTime(fechaDT);

                var tHeader   = marcaDBLocal.GetGeoAgrLaborAuxiliarAsync(compania!, idEmpleado!, fechaDT, idRegistroActual);
                var tDets = marcaDBLocal.GetGeoAgrLaborAuxiliarDetAsync(compania!, idEmpleado!, fechaDT, idRegistroActual);
                var tEmpAsig  = marcaDBLocal.GetGeoAgrLaborAuxiliarDetEmpAsigAsync(compania!, idEmpleado!, fechaDT,idRegistroActual);
                var tEstados = marcaDBLocal.GetEstadoAsync();


                await Task.WhenAll(tFincas, tLotes, tOts, tOtLabores, tHeader, tDets, tEmpAsig, empleadosTask, tRecursoConcepto, tEstados);

                encabezado.Estado = tEstados.Result?.FirstOrDefault(e => e.Id == tHeader.Result?.EstadoId)?.Descripcion ?? "Desconocido";
                listaFincas    = tFincas.Result;
                listaLotes     = tLotes.Result;
                listaOts       = tOts.Result;
                listaOtLabores = tOtLabores.Result;
                listaRecursoConcepto = tRecursoConcepto.Result;

                if (tHeader.Result is not null)
                    encabezado.Comentarios = tHeader.Result.Comentarios;

                if (tDets.Result is not null)
                {
                    detalles = tDets.Result.Select(d => new GeoAgrLaborAuxiliarDetExt
                    {
                        Compania            = d.Compania,
                        IdEmpleado          = d.IdEmpleado,
                        Fecha               = d.Fecha,
                        Item                = d.Item,
                        Bloque              = d.Bloque,
                        Finca               = d.Finca,
                        Lote                = d.Lote,
                        Ot                  = d.Ot,
                        Labor               = d.Labor,
                        Recurso             = d.Recurso,
                        CantidadRendimiento = d.CantidadRendimiento,
                        Unidad              = d.Unidad,
                        HorasAprobadas      = d.HorasAprobadas,
                        EstadoId = d.EstadoId,
                        // Resolver nombres desde los catálogos ya cargados
                        NombreOt    = tOts.Result?.FirstOrDefault(o => o.Ot == d.Ot && o.Bloque == d.Bloque && o.Finca == d.Finca)?.NombreOt ?? "",
                        NombreLabor = tOtLabores.Result?.FirstOrDefault(l => l.Labor == d.Labor && l.Bloque == d.Bloque && l.Finca == d.Finca && l.Ot == d.Ot)?.NombreLabor ?? "",
                        NombreRecurso = tRecursoConcepto.Result?.FirstOrDefault(r => r.Recurso == d.Recurso)?.Descripcion ?? "",
                    }).ToList();
                }

                // Pre-marcar los empleados que ya estaban asignados en sesiones anteriores
                if (tEmpAsig.Result is { Count: > 0 })
                {
                    var asignados = tEmpAsig.Result
                        .Select(a => a.IdEmpleadoAsignado)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    empleadosSeleccionados = empleados
                        .Where(e => asignados.Contains(e.IdNumero ?? ""))
                        .ToList();
                }
            }
            else
            {
                // Modo nuevo: solo los 3 catálogos en paralelo
                await Task.WhenAll(tFincas, tLotes, tOts, tOtLabores, empleadosTask, tRecursoConcepto);

                listaFincas    = tFincas.Result;
                listaLotes     = tLotes.Result;
                listaOtLabores = tOtLabores.Result;
                listaRecursoConcepto = tRecursoConcepto.Result;
                listaOts       = tOts.Result;

                encabezado.Compania   = loginState.Empleado.IdCCosto;
                encabezado.IdEmpleado = loginState.Empleado.IdNumero;
            }
        }
        catch (Exception ex)
        {
            await swal.FireAsync("Error", "Al inicializar el formulario: " + ex.Message, SweetAlertIcon.Error);
        }
        finally
        {
            InProcess = false;
        }
    }
```

with:

```csharp
    protected override async Task OnInitializedAsync()
    {
        try
        {
            // ── Catálogos: independientes entre sí → se lanzan sin await ─────────
            var tFincas    = marcaDBLocal.GetGeoAgrFincaAsync(loginState.CompaniaSeleccionada, loginState.PaisSeleccionado);
            var tLotes     = marcaDBLocal.GetGeoAgrLoteAsync(loginState.CompaniaSeleccionada);
            var tOts       = marcaDBLocal.GetGeoAgrOtAsync(loginState.CompaniaSeleccionada);
            var tOtLabores = marcaDBLocal.GetGeoAgrOtLaborAsync(loginState.CompaniaSeleccionada);
            var tRecursoConcepto = marcaDBLocal.GetGeoAgrRecursoConceptoAsync(loginState.CompaniaSeleccionada);

            var empleadosTask = CargarEmpleados(DateTime.Today);

            await Task.WhenAll(tFincas, tLotes, tOts, tOtLabores, empleadosTask, tRecursoConcepto);

            listaFincas    = tFincas.Result;
            listaLotes     = tLotes.Result;
            listaOtLabores = tOtLabores.Result;
            listaRecursoConcepto = tRecursoConcepto.Result;
            listaOts       = tOts.Result;

            encabezado.Compania   = loginState.Empleado.IdCCosto;
            encabezado.IdEmpleado = loginState.Empleado.IdNumero;
        }
        catch (Exception ex)
        {
            await swal.FireAsync("Error", "Al inicializar el formulario: " + ex.Message, SweetAlertIcon.Error);
        }
        finally
        {
            InProcess = false;
        }
    }
```

- [ ] **Step 5: Commit**

```bash
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor
git commit -m "feat: simplificar GeoAgrLaborAuxiliarDetalle a modo solo alta"
```

---

## Task 9: GeoAgrLaborAuxiliarDetalle.razor — expansión cartesiana ítem × empleado en `GuardarDatos`/`GuardarEnGeotime`

**Files:**
- Modify: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`

- [ ] **Step 1: Reescribir `GuardarDatos`**

In `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`, replace:

```csharp
    private async Task GuardarDatos()
    {
        InProcess = true;
        try
        {
            if (!await Validar()) return;

            var comp    = loginState.CompaniaSeleccionada;
            var fechaDT = encabezado.FechaDO.ToDateTime(TimeOnly.MinValue);

            var header = new GeoAgrLaborAuxiliar
            {
                Compania    = comp,
                IdEmpleado  = encabezado.IdEmpleado!,
                Fecha       = fechaDT,
                Comentarios = encabezado.Comentarios,
                EstadoId = 0,
            };

            // ── 1. Encabezado ────────────────────────────────────────────────
            // Nuevo → Insert (genera IdRegistro único); Edición → Update por IdRegistro
            if (esNuevo)
            {
                idRegistroActual = await marcaDBLocal.InsertGeoAgrLaborAuxiliarAsync(header);
            }
            else
            {
                await marcaDBLocal.UpdateGeoAgrLaborAuxiliarAsync(idRegistroActual, header);
            }

            // ── 2. Preparar listas de Det y EmpAsig ──────────────────────────
            var listaDets = detalles.Select(d => new GeoAgrLaborAuxiliarDet
            {
                Compania            = comp,
                IdEmpleado          = encabezado.IdEmpleado,
                Fecha               = fechaDT,
                Item                = d.Item,
                Bloque              = d.Bloque,
                Finca               = d.Finca,
                Lote                = d.Lote,
                Ot                  = d.Ot,
                Labor               = d.Labor,
                Recurso             = d.Recurso,
                CantidadRendimiento = d.CantidadRendimiento,
                Unidad              = d.Unidad,
                HorasAprobadas      = 0M,
                EstadoId            = d.EstadoId
            }).ToList();

            var listaEmpAsig = empleadosSeleccionados.Select(e => new GeoAgrLaborAuxiliarDetEmpAsig
            {
                Compania           = comp,
                IdEmpleado         = encabezado.IdEmpleado,
                Fecha              = fechaDT,
                IdEmpleadoAsignado = e.IdNumero,
                EstadoId = 0,
            }).ToList();

            // ── 3. Borrar hijos anteriores (por IdRegistro) en paralelo ──────
            await Task.WhenAll(
                marcaDBLocal.DeleteGeoAgrLaborAuxiliarDetAsync(idRegistroActual),
                marcaDBLocal.DeleteGeoAgrLaborAuxiliarDetEmpAsigAsync(idRegistroActual)
            );

            // ── 4. Guardar nuevos hijos (por IdRegistro) en paralelo ─────────
            var tareasDet = listaDets.Count > 0
                ? marcaDBLocal.SaveGeoAgrLaborAuxiliarDetAsync(idRegistroActual, listaDets)
                : Task.FromResult(0);

            var tareasEmp = listaEmpAsig.Count > 0
                ? marcaDBLocal.SaveGeoAgrLaborAuxiliarDetEmpAsigAsync(idRegistroActual, listaEmpAsig)
                : Task.FromResult(0);

            await Task.WhenAll(tareasDet, tareasEmp);

            await GuardarEnGeotime(header, listaDets, idRegistroActual, listaEmpAsig);

            await swal.FireAsync("Atención", "El proceso se realizó satisfactoriamente", SweetAlertIcon.Success);
            NavigationManager.NavigateTo(UrlRetorno);
        }
        catch (Exception ex)
        {
            await swal.FireAsync("Error", "Al guardar los datos: " + ex.Message, SweetAlertIcon.Error);
        }
        finally
        {
            InProcess = false;
        }
    }
```

with:

```csharp
    private async Task GuardarDatos()
    {
        InProcess = true;
        try
        {
            if (!await Validar()) return;

            var comp    = loginState.CompaniaSeleccionada;
            var fechaDT = encabezado.FechaDO.ToDateTime(TimeOnly.MinValue);

            var header = new GeoAgrLaborAuxiliar
            {
                Compania    = comp,
                IdEmpleado  = encabezado.IdEmpleado!,
                Fecha       = fechaDT,
                Comentarios = encabezado.Comentarios,
                EstadoId = 0,
            };

            // ── 1. Encabezado ────────────────────────────────────────────────
            idRegistroActual = await marcaDBLocal.InsertGeoAgrLaborAuxiliarAsync(header);

            // ── 2. Expansión cartesiana: un GeoAgrLaborAuxiliarDet por (Item × Empleado asignado) ──
            var listaDets = detalles.SelectMany(d => empleadosSeleccionados.Select(e => new GeoAgrLaborAuxiliarDet
            {
                Compania            = comp,
                IdEmpleado          = encabezado.IdEmpleado,
                Fecha               = fechaDT,
                Item                = d.Item,
                Bloque              = d.Bloque,
                Finca               = d.Finca,
                Lote                = d.Lote,
                Ot                  = d.Ot,
                Labor               = d.Labor,
                Recurso             = d.Recurso,
                CantidadRendimiento = d.CantidadRendimiento,
                Unidad              = d.Unidad,
                HorasAprobadas      = 0M,
                EstadoId            = d.EstadoId,
                IdEmpleadoAsignado  = e.IdNumero,
            })).ToList();

            // ── 3. Guardar detalles (alta, asociados al IdRegistro nuevo) ────
            await marcaDBLocal.SaveGeoAgrLaborAuxiliarDetAsync(idRegistroActual, listaDets);

            await GuardarEnGeotime(header, listaDets, idRegistroActual);

            await swal.FireAsync("Atención", "El proceso se realizó satisfactoriamente", SweetAlertIcon.Success);
            NavigationManager.NavigateTo(UrlRetorno);
        }
        catch (Exception ex)
        {
            await swal.FireAsync("Error", "Al guardar los datos: " + ex.Message, SweetAlertIcon.Error);
        }
        finally
        {
            InProcess = false;
        }
    }
```

- [ ] **Step 2: Reescribir `GuardarEnGeotime` con firma de 3 parámetros**

In `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`, replace:

```csharp
    private async Task GuardarEnGeotime(GeoAgrLaborAuxiliar header, List<GeoAgrLaborAuxiliarDet> listaDets, long idRegistroActual, List<GeoAgrLaborAuxiliarDetEmpAsig> listaEmpAsig)
    {
        try
        {
            if (!sincronizar)
                return; // Si no se puede sincronizar, salir sin hacer nada

            List<cSolicitudAuxLaborDet> listaDetalle = new List<cSolicitudAuxLaborDet>();

            foreach (var empleado in listaEmpAsig)
            {
                listaDetalle.AddRange(listaDets.Select(d => new cSolicitudAuxLaborDet
                {
                    SOLICITUDID = 0, // Se asignará en Geotime
                    IDNUMERO = empleado.IdEmpleadoAsignado!,
                    ITEM = d.Item,
                    BLOQUE = d.Bloque!,
                    FINCA = d.Finca!,
                    LOTE = d.Lote!,
                    OT = d.Ot!,
                    LABOR = d.Labor!,
                    RECURSO = d.Recurso!,
                    CANTIDAD_RENDIMIENTO = (decimal)d.CantidadRendimiento!,
                    UNIDAD = d.Unidad!,
                    COMPANIA = header.Compania!,
                    FECHA = header.Fecha,
                    HORAS_APROBADAS = 0M,
                    ESTADOID = d.EstadoId,
                }));

            }

            var laborGT = new cSolicitudAuxLabor
            {
                ID = 0, // Se asignará en Geotime
                COMPANIA    = header.Compania!,
                IDRESPONSABLE  = header.IdEmpleado!,
                FECHA       = header.Fecha,
                COMENTARIOS = header.Comentarios,
                ESTADOID = header.EstadoId,                
                cSolicitudAuxLaborDet = listaDetalle,
                FECHAREGISTRO = DateTime.Now,
                FECHAMODIFICA = DateTime.Now,
                IDREGISTROAPP = idRegistroActual, // Para vincular con nuestro registro local
                IDUSUARIOMODIFICA = loginState.Empleado.IdNumero!,
                IDUSUARIOREGISTRA = loginState.Empleado.IdNumero!,
            };
            var response = await geoConnect.Post("SolicitudAuxLabor", new List<cSolicitudAuxLabor>() { laborGT });

            if (response.Id=="0")
            {
                // Actualizar nuestro registro local con el ID asignado por Geotime
                await marcaDBLocal.SaveGeoAgrLaborAuxiliarEstadoAsync(idRegistroActual, header.Compania!);

            }

        }
        catch (Exception ex)
        {
            // No interrumpe el flujo principal, solo muestra una alerta
            await swal.FireAsync("Error", "Al sincronizar con Geotime: " + ex.Message, SweetAlertIcon.Error);
        }
    }
```

with:

```csharp
    private async Task GuardarEnGeotime(GeoAgrLaborAuxiliar header, List<GeoAgrLaborAuxiliarDet> listaDets, long idRegistroActual)
    {
        try
        {
            if (!sincronizar)
                return; // Si no se puede sincronizar, salir sin hacer nada

            var listaDetalle = listaDets.Select(d => new cSolicitudAuxLaborDet
            {
                SOLICITUDID = 0, // Se asignará en Geotime
                IDNUMERO = d.IdEmpleadoAsignado!,
                ITEM = d.Item,
                BLOQUE = d.Bloque!,
                FINCA = d.Finca!,
                LOTE = d.Lote!,
                OT = d.Ot!,
                LABOR = d.Labor!,
                RECURSO = d.Recurso!,
                CANTIDAD_RENDIMIENTO = (decimal)d.CantidadRendimiento!,
                UNIDAD = d.Unidad!,
                COMPANIA = header.Compania!,
                FECHA = header.Fecha,
                HORAS_APROBADAS = 0M,
                ESTADOID = d.EstadoId,
            }).ToList();

            var laborGT = new cSolicitudAuxLabor
            {
                ID = 0, // Se asignará en Geotime
                COMPANIA    = header.Compania!,
                IDRESPONSABLE  = header.IdEmpleado!,
                FECHA       = header.Fecha,
                COMENTARIOS = header.Comentarios,
                ESTADOID = header.EstadoId,
                cSolicitudAuxLaborDet = listaDetalle,
                FECHAREGISTRO = DateTime.Now,
                FECHAMODIFICA = DateTime.Now,
                IDREGISTROAPP = idRegistroActual, // Para vincular con nuestro registro local
                IDUSUARIOMODIFICA = loginState.Empleado.IdNumero!,
                IDUSUARIOREGISTRA = loginState.Empleado.IdNumero!,
            };
            var response = await geoConnect.Post("SolicitudAuxLabor", new List<cSolicitudAuxLabor>() { laborGT });

            if (response.Id=="0")
            {
                // Actualizar nuestro registro local con el ID asignado por Geotime
                await marcaDBLocal.SaveGeoAgrLaborAuxiliarEstadoAsync(idRegistroActual, header.Compania!);

            }

        }
        catch (Exception ex)
        {
            // No interrumpe el flujo principal, solo muestra una alerta
            await swal.FireAsync("Error", "Al sincronizar con Geotime: " + ex.Message, SweetAlertIcon.Error);
        }
    }
```

> **Nota para el implementador:** la línea `ESTADOID = header.EstadoId,` en el archivo real puede tener espacios finales adicionales. Si el `old_string` no calza exactamente, use `Read` sobre `GeoAgrLaborAuxiliarDetalle.razor:649-711` para obtener el texto exacto y ajuste el `old_string` en consecuencia — el `new_string` de arriba es el resultado final correcto.

- [ ] **Step 3: Commit**

```bash
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor
git commit -m "feat: expansion cartesiana item x empleado en GuardarDatos/GuardarEnGeotime"
```

---

## Task 10: Nuevo componente `GeoAgrLaborAuxiliarEdicion.razor`

**Files:**
- Create: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarEdicion.razor`

- [ ] **Step 1: Crear el archivo**

Create `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarEdicion.razor` with:

```razor
@page "/geoagrlaborauxiliaredicion"
@using PortalEmpleadoGeotime.Client.Data.DataBaseLocal

<div class="card">
    <PageReturn Titulo="@Titulo" UrlRetorno="@UrlRetorno" />
    <div class="card-body px-2 py-2">
        @if (InProcess)
        {
            <SpinnerControl></SpinnerControl>
        }

        <div class="ibm-plex-serif-bold">

            @* ══ Encabezado ══ *@
            <RadzenPanel AllowCollapse="true" class="panel-brand" Style="margin-bottom:0.5rem;">
                <HeaderTemplate>
                    <RadzenIcon Icon="event" class="me-1" /> Datos de la Labor
                </HeaderTemplate>
                <ChildContent>
                    <div style="display:flex;flex-direction:column;gap:8px;">
                        <label class="lbl-xs" style="display:flex; align-items:end; gap:10px;">
                            <div style="display:flex; flex-direction:column; gap:3px;">
                                <span>Registro No.</span>
                                <div class="form-control br8" style="background:#f5f5f5;">@idRegistroActual</div>
                            </div>
                            <span class="emp-status-badge enrolled">
                                <RadzenIcon Icon="task_alt" Style="font-size:0.78rem;" />@encabezado.Estado
                            </span>
                        </label>
                        <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                            Fecha
                            <div class="form-control br8" style="background:#f5f5f5;">@encabezado.Fecha.ToString("dd/MM/yyyy")</div>
                        </label>
                        <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                            Empleado responsable
                            <div class="form-control br8" style="background:#f5f5f5;">@encabezado.NombreEmpleado</div>
                        </label>
                        <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                            Comentarios
                            <div class="form-control br8" style="background:#f5f5f5;min-height:38px;">@encabezado.Comentarios</div>
                        </label>
                    </div>
                </ChildContent>
            </RadzenPanel>

            @* ══ Ítems asignados ══ *@
            <RadzenPanel AllowCollapse="true" class="panel-brand" Style="margin-bottom:0.5rem;">
                <HeaderTemplate>
                    <RadzenIcon Icon="agriculture" class="me-1" /> Ítems asignados
                    @if (items.Count > 0)
                    {
                        <span style="margin-left:6px;display:inline-flex;align-items:center;gap:3px;
                                     padding:1px 8px;border-radius:12px;
                                     background:#e8f5e9;font-size:0.68rem;font-weight:600;color:#017F01;">
                            @items.Count @(items.Count == 1 ? "ítem" : "ítems")
                        </span>
                    }
                </HeaderTemplate>
                <ChildContent>
                    @if (items.Count > 0)
                    {
                        @foreach (var item in items)
                        {
                            <div style="background:#fff;border:1px solid #e0e0e0;border-radius:10px;
                                        border-top:3px solid #017F01;padding:12px;margin-bottom:8px;">
                                <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:10px;">
                                    <span style="display:inline-flex;align-items:center;gap:4px;padding:2px 10px;border-radius:12px;
                                                 background:#e8f5e9;font-size:0.72rem;font-weight:700;color:#017F01;">
                                        <RadzenIcon Icon="agriculture" Style="font-size:0.78rem;" />Ítem @item.Item
                                    </span>
                                </div>

                                <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:10px;">
                                    <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                                        Bloque/Finca
                                        <div class="form-control br8" style="background:#f5f5f5;font-size:0.8rem;">@item.Bloque / @item.Finca</div>
                                    </label>
                                    <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                                        Lote
                                        <div class="form-control br8" style="background:#f5f5f5;font-size:0.8rem;">@item.Lote</div>
                                    </label>
                                    <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                                        OT
                                        <div class="form-control br8" style="background:#f5f5f5;font-size:0.8rem;">@item.Ot - @item.NombreOt</div>
                                    </label>
                                    <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                                        Labor
                                        <div class="form-control br8" style="background:#f5f5f5;font-size:0.8rem;">@item.Labor - @item.NombreLabor</div>
                                    </label>
                                    <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                                        Recurso
                                        <div class="form-control br8" style="background:#f5f5f5;font-size:0.8rem;">@item.Recurso - @item.NombreRecurso</div>
                                    </label>
                                    <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                                        Unidad
                                        <div class="form-control br8" style="background:#f5f5f5;font-size:0.8rem;">@item.Unidad</div>
                                    </label>
                                </div>

                                @foreach (var emp in item.Empleados)
                                {
                                    <div style="display:flex;gap:10px;align-items:flex-start;padding:8px 0;border-top:1px solid #f0f0f0;">
                                        <div class="emp-avatar" style="width:36px;height:36px;font-size:0.85rem;flex-shrink:0;">
                                            @(string.IsNullOrEmpty(emp.NombreEmpleado) ? "?" : emp.NombreEmpleado.Substring(0,1).ToUpper())
                                        </div>
                                        <div style="flex:1;min-width:0;">
                                            <div style="font-weight:700;font-size:0.85rem;color:#212121;line-height:1.3;">@emp.NombreEmpleado</div>
                                            <div style="font-size:0.7rem;color:#999;font-variant-numeric:tabular-nums;margin-bottom:6px;">@emp.IdEmpleadoAsignado</div>

                                            <div style="display:flex;flex-direction:column;gap:6px;">
                                                <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                                                    Asignada
                                                    <RadzenNumeric TValue="decimal?"
                                                                   @bind-Value="emp.CantidadAsignada"
                                                                   Placeholder="0.00"
                                                                   Format="n2"
                                                                   Style="font-size:small;width:100%;" />
                                                </label>
                                                <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                                                    Aprobada
                                                    <div class="form-control br8" style="background:#f5f5f5;font-size:0.8rem;">@(emp.CantidadAprobada?.ToString("n2") ?? "0.00")</div>
                                                </label>
                                                <label class="lbl-xs" style="display:flex;flex-direction:column;gap:3px;">
                                                    Estado
                                                    <span class="emp-status-badge enrolled" style="align-self:flex-start;">
                                                        <RadzenIcon Icon="task_alt" Style="font-size:0.78rem;" />@emp.EstadoDescripcion
                                                    </span>
                                                </label>
                                            </div>
                                        </div>
                                    </div>
                                }

                                <RadzenButton Icon="save"
                                              Text="@($"Guardar cambios del ítem {item.Item}")"
                                              ButtonStyle="ButtonStyle.Primary"
                                              Size="ButtonSize.Medium"
                                              Style="width:100%;touch-action:manipulation;margin-top:10px;"
                                              Click="@(() => GuardarItem(item))" />
                            </div>
                        }
                    }
                    else
                    {
                        <div style="text-align:center;padding:24px 0;color:#bbb;">
                            <RadzenIcon Icon="agriculture" Style="font-size:2.5rem;display:block;margin:0 auto 8px;color:#ccc;" />
                            <div style="font-size:0.85rem;color:#999;">No hay ítems registrados.</div>
                        </div>
                    }
                </ChildContent>
            </RadzenPanel>

        </div>
    </div>
</div>

@code {

    [SupplyParameterFromQuery] public string? compania    { get; set; }
    [SupplyParameterFromQuery] public string? idEmpleado  { get; set; }
    [SupplyParameterFromQuery] public string? fecha       { get; set; }
    [SupplyParameterFromQuery] public long?   idregistro  { get; set; }

    private long idRegistroActual = 0;
    private string Titulo     = "Labor Auxiliar";
    private string UrlRetorno = "./geoagrlaborauxiliarlist";
    bool InProcess = true;

    private EncabezadoVM encabezado = new();
    private List<GeoAgrLaborAuxiliarItemVM> items = new();

    MarcasDBLocal marcaDBLocal = new();

    public class EncabezadoVM
    {
        public DateTime Fecha { get; set; }
        public string? NombreEmpleado { get; set; }
        public string? Comentarios { get; set; }
        public string Estado { get; set; } = "";
    }

    public class GeoAgrLaborAuxiliarItemVM
    {
        public int Item { get; set; }
        public string? Bloque { get; set; }
        public string? Finca { get; set; }
        public string? Lote { get; set; }
        public string? Ot { get; set; }
        public string NombreOt { get; set; } = "";
        public string? Labor { get; set; }
        public string NombreLabor { get; set; } = "";
        public string? Recurso { get; set; }
        public string NombreRecurso { get; set; } = "";
        public string? Unidad { get; set; }
        public List<GeoAgrLaborAuxiliarEmpleadoVM> Empleados { get; set; } = new();
    }

    public class GeoAgrLaborAuxiliarEmpleadoVM
    {
        public string IdEmpleadoAsignado { get; set; } = "";
        public string NombreEmpleado { get; set; } = "";
        public decimal? CantidadAsignada { get; set; } // editable → CantidadRendimiento
        public decimal? CantidadAprobada { get; set; } // solo lectura → HorasAprobadas
        public string EstadoDescripcion { get; set; } = "";
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            idRegistroActual = idregistro ?? 0;
            var fechaDT = DateTime.ParseExact(fecha!, "yyyyMMdd", null);

            var tHeader    = marcaDBLocal.GetGeoAgrLaborAuxiliarAsync(compania!, idEmpleado!, fechaDT, idRegistroActual);
            var tDets      = marcaDBLocal.GetGeoAgrLaborAuxiliarDetAsync(compania!, idEmpleado!, fechaDT, idRegistroActual);
            var tEmpleados = marcaDBLocal.GetEmpleadoAsync();
            var tEstados   = marcaDBLocal.GetEstadoAsync();
            var tOts       = marcaDBLocal.GetGeoAgrOtAsync(loginState.CompaniaSeleccionada);
            var tOtLabores = marcaDBLocal.GetGeoAgrOtLaborAsync(loginState.CompaniaSeleccionada);
            var tRecursos  = marcaDBLocal.GetGeoAgrRecursoConceptoAsync(loginState.CompaniaSeleccionada);

            await Task.WhenAll(tHeader, tDets, tEmpleados, tEstados, tOts, tOtLabores, tRecursos);

            var nombresPorId = (tEmpleados.Result ?? Enumerable.Empty<cEmpleado>())
                .Where(e => !string.IsNullOrEmpty(e.IdNumero))
                .ToDictionary(e => e.IdNumero!, e => e.Nombre ?? "");

            var estadosPorId = (tEstados.Result ?? Enumerable.Empty<cEstado>())
                .ToDictionary(e => e.Id, e => e.Descripcion);

            if (tHeader.Result is not null)
            {
                encabezado.Fecha = tHeader.Result.Fecha;
                encabezado.Comentarios = tHeader.Result.Comentarios;
                encabezado.Estado = estadosPorId.TryGetValue(tHeader.Result.EstadoId, out var estHeader) ? estHeader : "Desconocido";
                encabezado.NombreEmpleado = nombresPorId.TryGetValue(idEmpleado ?? "", out var nombreResp) ? nombreResp : idEmpleado ?? "";
            }

            if (tDets.Result is not null)
            {
                items = tDets.Result
                    .GroupBy(d => d.Item)
                    .OrderBy(g => g.Key)
                    .Select(g =>
                    {
                        var primero = g.First();
                        return new GeoAgrLaborAuxiliarItemVM
                        {
                            Item    = g.Key,
                            Bloque  = primero.Bloque,
                            Finca   = primero.Finca,
                            Lote    = primero.Lote,
                            Ot      = primero.Ot,
                            NombreOt = tOts.Result?.FirstOrDefault(o => o.Ot == primero.Ot && o.Bloque == primero.Bloque && o.Finca == primero.Finca)?.NombreOt ?? "",
                            Labor   = primero.Labor,
                            NombreLabor = tOtLabores.Result?.FirstOrDefault(l => l.Labor == primero.Labor && l.Bloque == primero.Bloque && l.Finca == primero.Finca && l.Ot == primero.Ot)?.NombreLabor ?? "",
                            Recurso = primero.Recurso,
                            NombreRecurso = tRecursos.Result?.FirstOrDefault(r => r.Recurso == primero.Recurso)?.Descripcion ?? "",
                            Unidad  = primero.Unidad,
                            Empleados = g.Select(d => new GeoAgrLaborAuxiliarEmpleadoVM
                            {
                                IdEmpleadoAsignado = d.IdEmpleadoAsignado ?? "",
                                NombreEmpleado = nombresPorId.TryGetValue(d.IdEmpleadoAsignado ?? "", out var nombreEmp) ? nombreEmp : d.IdEmpleadoAsignado ?? "",
                                CantidadAsignada = d.CantidadRendimiento,
                                CantidadAprobada = d.HorasAprobadas,
                                EstadoDescripcion = estadosPorId.TryGetValue(d.EstadoId, out var estDet) ? estDet : "Desconocido",
                            }).ToList(),
                        };
                    })
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            await swal.FireAsync("Error", "Al inicializar el formulario: " + ex.Message, SweetAlertIcon.Error);
        }
        finally
        {
            InProcess = false;
        }
    }

    private async Task GuardarItem(GeoAgrLaborAuxiliarItemVM itemVM)
    {
        InProcess = true;
        try
        {
            var cantidades = itemVM.Empleados.Select(e => (e.IdEmpleadoAsignado, e.CantidadAsignada));
            await marcaDBLocal.UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync(idRegistroActual, itemVM.Item, cantidades);
            await swal.FireAsync("Atención", "Cambios guardados", SweetAlertIcon.Success);
        }
        catch (Exception ex)
        {
            await swal.FireAsync("Error", "Al guardar los cambios: " + ex.Message, SweetAlertIcon.Error);
        }
        finally
        {
            InProcess = false;
        }
    }
}
```

- [ ] **Step 2: Verificar el build**

Run: `dotnet build LaboresNet/LaboresNet.csproj -f net10.0-windows10.0.19041.0`

Este archivo es nuevo y autocontenido: solo usa métodos de `MarcasDBLocal` que ya existen (`GetGeoAgrLaborAuxiliarAsync`, `GetGeoAgrLaborAuxiliarDetAsync`, `GetEmpleadoAsync`, `GetEstadoAsync`, `GetGeoAgrOtAsync`, `GetGeoAgrOtLaborAsync`, `GetGeoAgrRecursoConceptoAsync`) más el nuevo `UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync` (Tarea 6). Los errores restantes deben ser **únicamente** los de `GeoAgrLaborAuxiliarList.razor` y `SincronizacionManual.razor` ya identificados en la Tarea 7 (las Tareas 11-12 los resuelven). Si aparece algún error en `GeoAgrLaborAuxiliarEdicion.razor`, corríjalo antes de continuar.

- [ ] **Step 3: Commit**

```bash
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarEdicion.razor
git commit -m "feat: nueva pagina GeoAgrLaborAuxiliarEdicion para ajustar cantidad asignada por empleado"
```

---

## Task 11: `GeoAgrLaborAuxiliarList.razor` — actualizar conteos y ruta de edición

**Files:**
- Modify: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor:206-222` y `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor:239-243`

- [ ] **Step 1: Recalcular `CantDetalles`/`CantEmpleados` a partir de `detalles`**

En `GetListaItems()`, eliminar la llamada a `GetGeoAgrLaborAuxiliarDetEmpAsigAsync` (ya no existe `GeoAgrLaborAuxiliarDetEmpAsig`) y calcular ambos conteos a partir de `detalles`, usando `Item` distintos para `CantDetalles` e `IdEmpleadoAsignado` distintos para `CantEmpleados`.

Old:
```csharp
                foreach (var r in filtrados)
                {
                    var detalles = await marcaDBLocal.GetGeoAgrLaborAuxiliarDetAsync(r.Compania!, r.IdEmpleado!, r.Fecha, r.IdRegistro);
                    var empleados = await marcaDBLocal.GetGeoAgrLaborAuxiliarDetEmpAsigAsync(r.Compania!, r.IdEmpleado!, r.Fecha, r.IdRegistro);

                    items.Add(new GeoAgrLaborAuxiliarExt
                    {
                        IdRegistro   = r.IdRegistro,
                        Compania     = r.Compania,
                        IdEmpleado   = r.IdEmpleado,
                        Fecha        = r.Fecha,
                        Comentarios  = r.Comentarios,
                        CantDetalles = detalles?.Count() ?? 0,
                        CantEmpleados = empleados?.Count() ?? 0,
                        Filtro = $"{r.IdRegistro} {r.Fecha:dd/MM/yyyy} {r.IdEmpleado} {r.Comentarios}"
                    });
                }
```

New:
```csharp
                foreach (var r in filtrados)
                {
                    var detalles = await marcaDBLocal.GetGeoAgrLaborAuxiliarDetAsync(r.Compania!, r.IdEmpleado!, r.Fecha, r.IdRegistro);

                    items.Add(new GeoAgrLaborAuxiliarExt
                    {
                        IdRegistro   = r.IdRegistro,
                        Compania     = r.Compania,
                        IdEmpleado   = r.IdEmpleado,
                        Fecha        = r.Fecha,
                        Comentarios  = r.Comentarios,
                        CantDetalles = detalles?.Select(d => d.Item).Distinct().Count() ?? 0,
                        CantEmpleados = detalles?.Select(d => d.IdEmpleadoAsignado).Distinct().Count() ?? 0,
                        Filtro = $"{r.IdRegistro} {r.Fecha:dd/MM/yyyy} {r.IdEmpleado} {r.Comentarios}"
                    });
                }
```

- [ ] **Step 2: Cambiar la ruta de edición a `geoagrlaborauxiliaredicion`**

`OnClickAdd` (líneas 234-237) no cambia — sigue navegando a `./geoagrlaborauxiliardetalle` para crear un registro nuevo. Solo `OnClickEdit` cambia su destino al nuevo componente de edición.

Old:
```csharp
    private void OnClickEdit(GeoAgrLaborAuxiliarExt labor)
    {
        var fecha = labor.Fecha.ToString("yyyyMMdd");
        NavigationManager.NavigateTo($"./geoagrlaborauxiliardetalle?compania={Uri.EscapeDataString(labor.Compania ?? "")}&idEmpleado={Uri.EscapeDataString(labor.IdEmpleado ?? "")}&fecha={fecha}&idregistro={labor.IdRegistro}");
    }
```

New:
```csharp
    private void OnClickEdit(GeoAgrLaborAuxiliarExt labor)
    {
        var fecha = labor.Fecha.ToString("yyyyMMdd");
        NavigationManager.NavigateTo($"./geoagrlaborauxiliaredicion?compania={Uri.EscapeDataString(labor.Compania ?? "")}&idEmpleado={Uri.EscapeDataString(labor.IdEmpleado ?? "")}&fecha={fecha}&idregistro={labor.IdRegistro}");
    }
```

- [ ] **Step 3: Verificar el build**

Run: `dotnet build LaboresNet/LaboresNet.csproj -f net10.0-windows10.0.19041.0`

Expected: los errores relacionados con `GeoAgrLaborAuxiliarList.razor` identificados en la Tarea 7 ya no aparecen. Los únicos errores restantes deben ser los de `SincronizacionManual.razor` (resueltos en la Tarea 12).

- [ ] **Step 4: Commit**

```bash
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor
git commit -m "feat: actualizar GeoAgrLaborAuxiliarList con nuevos conteos y ruta de edicion"
```

---

## Task 12: `SincronizacionManual.razor` — filtrar sincronización de estado por `IdEmpleadoAsignado`

**Files:**
- Modify: `LaboresNet/Components/Pages/Procesos/SincronizacionManual.razor:135-253` (aprox.)

> **Nota para el implementador:** este archivo usa indentación con tabulaciones (`\t`). Los bloques `Old`/`New` de abajo reproducen la indentación tal como se observó con `Read`, pero si el `old_string` no calza exactamente al aplicar el `Edit`, use `Read` sobre el rango de líneas indicado para obtener el texto exacto (incluyendo tabs/espacios) y ajuste el `old_string` en consecuencia — el `new_string` de cada paso es el resultado final correcto.

- [ ] **Step 1: Eliminar la tarea `itemsLaborAuxiliarEmpTask`**

`GeoAgrLaborAuxiliarDetEmpAsig` ya no existe (Tareas 2/3), por lo que `GetGeoAgrLaborAuxiliarDetEmpAsigAsync` tampoco. Eliminar su declaración.

Old (línea ~135-137):
```csharp
			var laborAuxiliarTask = marcaDBLocal.GetGeoAgrLaborAuxiliarAsync(loginState.CompaniaSeleccionada, loginState.Empleado.IdNumero!, DateTime.Now.AddDays(-35), DateTime.Now.AddDays(1));
			var itemsLaborAuxiliarTask = marcaDBLocal.GetGeoAgrLaborAuxiliarDetAsync(loginState.CompaniaSeleccionada, loginState.Empleado.IdNumero!, DateTime.Now.AddDays(-35), DateTime.Now.AddDays(1));
			var itemsLaborAuxiliarEmpTask = marcaDBLocal.GetGeoAgrLaborAuxiliarDetEmpAsigAsync(loginState.CompaniaSeleccionada, loginState.Empleado.IdNumero!, DateTime.Now.AddDays(-35), DateTime.Now.AddDays(1));
```

New:
```csharp
			var laborAuxiliarTask = marcaDBLocal.GetGeoAgrLaborAuxiliarAsync(loginState.CompaniaSeleccionada, loginState.Empleado.IdNumero!, DateTime.Now.AddDays(-35), DateTime.Now.AddDays(1));
			var itemsLaborAuxiliarTask = marcaDBLocal.GetGeoAgrLaborAuxiliarDetAsync(loginState.CompaniaSeleccionada, loginState.Empleado.IdNumero!, DateTime.Now.AddDays(-35), DateTime.Now.AddDays(1));
```

- [ ] **Step 2: Quitar `itemsLaborAuxiliarEmpTask` de `Task.WhenAll`**

Old (línea ~150):
```csharp
			await Task.WhenAll(empleadosTask, marcasTask, phPlanillasTask, phPeriodosTask,
									marcasPendientesTask, ccostosTask, proyectosTask, faseproyectosTask,
									phUsuarioTask, phLoginTask, distribucionPendienteTask, templatesFACESTask,
									fincasTask, lotesTask, otsTask, laborAuxiliarTask, itemsLaborAuxiliarTask, itemsLaborAuxiliarEmpTask,
									otLaboresTask, recursosTask, solicitudAuxLaborTask, estadosTask);
```

New:
```csharp
			await Task.WhenAll(empleadosTask, marcasTask, phPlanillasTask, phPeriodosTask,
									marcasPendientesTask, ccostosTask, proyectosTask, faseproyectosTask,
									phUsuarioTask, phLoginTask, distribucionPendienteTask, templatesFACESTask,
									fincasTask, lotesTask, otsTask, laborAuxiliarTask, itemsLaborAuxiliarTask,
									otLaboresTask, recursosTask, solicitudAuxLaborTask, estadosTask);
```

- [ ] **Step 3: Eliminar el `await` de `itemsLaborAuxiliarEmpTask`**

Old (línea ~172-176):
```csharp
			var solicitudAuxLabor = await solicitudAuxLaborTask;
			var itemsLaborAuxiliar = await itemsLaborAuxiliarTask;
			var itemsLaborAuxiliarEmp = await itemsLaborAuxiliarEmpTask;

			List<GeoAgrLaborAuxiliar>? laboresAuxiliares = await laborAuxiliarTask;
```

New:
```csharp
			var solicitudAuxLabor = await solicitudAuxLaborTask;
			var itemsLaborAuxiliar = await itemsLaborAuxiliarTask;

			List<GeoAgrLaborAuxiliar>? laboresAuxiliares = await laborAuxiliarTask;
```

- [ ] **Step 4: Filtrar `itemsAuxDet` por `Item` e `IdEmpleadoAsignado`, eliminar el sub-bloque EmpAsig**

Con la expansión cartesiana (Tarea 9), `itemsLaborAuxiliar` (= `GetGeoAgrLaborAuxiliarDetAsync`) tiene una fila por ítem × empleado asignado, y `cSolicitudAuxLaborDet` (`itemDet`) también es por ítem × empleado (`ITEM` + `IDNUMERO`, ver spec §5.3). El filtro de `itemsAuxDet` debe incluir ambos campos para actualizar solo la fila del empleado correspondiente.

Old (línea ~220-253):
```csharp
			foreach (var solicitud in solicitudAuxLabor)
			{
				if (laboresAuxiliares.Any(l => l.IdRegistro == solicitud.IDREGISTROAPP))
				{
					var itemsSolicitudAprobadas = solicitud.cSolicitudAuxLaborDet!.Where(i => i.ESTADOID == 99);
					foreach (var itemDet in itemsSolicitudAprobadas)
					{
						var itemsAuxDet = itemsLaborAuxiliar.Where(i => i.IdRegistro == solicitud.IDREGISTROAPP && i.Item == itemDet.ITEM).ToList();
						List<GeoAgrLaborAuxiliarDet> itemsAuxDetActualizar = new List<GeoAgrLaborAuxiliarDet>();
						foreach (var itemAux in itemsAuxDet)
						{
							itemAux.EstadoId = 99;						
							itemsAuxDetActualizar.Add(itemAux);
						}

						if (itemsAuxDetActualizar.Count > 0)
							await marcaDBLocal.SaveGeoAgrLaborAuxiliarDetAsync(itemsAuxDetActualizar);

						var itemsAuxDetEmp = itemsLaborAuxiliarEmp.Where(i => i.IdRegistro == solicitud.IDREGISTROAPP).ToList();
						List<GeoAgrLaborAuxiliarDetEmpAsig> itemsAuxDetEmpActualizar = new List<GeoAgrLaborAuxiliarDetEmpAsig>();
						foreach (var itemAuxEmp in itemsAuxDetEmp)
						{
							itemAuxEmp.EstadoId = 99;
                            itemsAuxDetEmpActualizar.Add(itemAuxEmp);							
						}
						await marcaDBLocal.SaveGeoAgrLaborAuxiliarDetEmpAsigAsync(itemsAuxDetEmpActualizar);

					}
					
					
				}
				
				
			}
```

New:
```csharp
			foreach (var solicitud in solicitudAuxLabor)
			{
				if (laboresAuxiliares.Any(l => l.IdRegistro == solicitud.IDREGISTROAPP))
				{
					var itemsSolicitudAprobadas = solicitud.cSolicitudAuxLaborDet!.Where(i => i.ESTADOID == 99);
					foreach (var itemDet in itemsSolicitudAprobadas)
					{
						var itemsAuxDet = itemsLaborAuxiliar
							.Where(i => i.IdRegistro == solicitud.IDREGISTROAPP
									 && i.Item == itemDet.ITEM
									 && i.IdEmpleadoAsignado == itemDet.IDNUMERO)
							.ToList();
						List<GeoAgrLaborAuxiliarDet> itemsAuxDetActualizar = new List<GeoAgrLaborAuxiliarDet>();
						foreach (var itemAux in itemsAuxDet)
						{
							itemAux.EstadoId = 99;						
							itemsAuxDetActualizar.Add(itemAux);
						}

						if (itemsAuxDetActualizar.Count > 0)
							await marcaDBLocal.SaveGeoAgrLaborAuxiliarDetAsync(itemsAuxDetActualizar);
					}
					
					
				}
				
				
			}
```

- [ ] **Step 5: Verificar el build**

Run: `dotnet build LaboresNet/LaboresNet.csproj -f net10.0-windows10.0.19041.0`

Expected: `Build succeeded. 0 Error(s)`. Este es el último archivo con referencias pendientes a `GeoAgrLaborAuxiliarDetEmpAsig` / `GetGeoAgrLaborAuxiliarDetEmpAsigAsync` / `SaveGeoAgrLaborAuxiliarDetEmpAsigAsync` (Tarea 7); tras este cambio el proyecto debe compilar sin errores.

- [ ] **Step 6: Commit**

```bash
git add LaboresNet/Components/Pages/Procesos/SincronizacionManual.razor
git commit -m "feat: actualizar filtro de sincronizacion de estado por IdEmpleadoAsignado"
```

---

## Task 13: Build final y verificación end-to-end

**Files:** (ninguno — solo verificación)

- [ ] **Step 1: Build final**

Run: `dotnet build LaboresNet/LaboresNet.csproj -f net10.0-windows10.0.19041.0`

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 2: Verificación cruzada de tipos y firmas**

Confirmar que los siguientes nombres son consistentes en todo el proyecto (todos definidos/usados en las Tareas 1, 2, 4, 5, 6, 9, 10, 11, 12):

- `GeoAgrLaborAuxiliarDet.IdEmpleadoAsignado` (Tarea 1) y `GeoAgrLaborAuxiliarDetLocal.IdEmpleadoAsignado` (Tarea 2) — ambos `string?`.
- `MapDetLocalToDet` (Tarea 4, Step 1) mapea `IdEmpleadoAsignado = l.IdEmpleadoAsignado`.
- `SaveGeoAgrLaborAuxiliarDetAsync(IEnumerable<GeoAgrLaborAuxiliarDet>)` (Tarea 4, Step 2) usa la clave `(Compania, IdEmpleado, Fecha, Item, IdEmpleadoAsignado)` con fallback `idEmpAsig = item.IdEmpleadoAsignado ?? item.IdEmpleado`.
- `SaveGeoAgrLaborAuxiliarDetAsync(long idRegistro, IEnumerable<GeoAgrLaborAuxiliarDet>)` (Tarea 5) mapea `IdEmpleadoAsignado = item.IdEmpleadoAsignado`.
- `UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync(long idRegistro, int item, IEnumerable<(string IdEmpleadoAsignado, decimal? CantidadAsignada)> cantidades)` (Tarea 6, Step 2) — la firma usada en `GeoAgrLaborAuxiliarEdicion.razor` (`GuardarItem`, Tarea 10) debe coincidir exactamente.
- `GuardarEnGeotime(GeoAgrLaborAuxiliar header, List<GeoAgrLaborAuxiliarDet> listaDets, long idRegistroActual)` (Tarea 9, Step 2) — 3 parámetros, sin `listaEmpAsig`; llamado así desde `GuardarDatos` (Tarea 9, Step 1).
- `GeoAgrLaborAuxiliarItemVM` / `GeoAgrLaborAuxiliarEmpleadoVM` / `EncabezadoVM` (Tarea 10) — usados consistentemente en el markup y en `OnInitializedAsync`/`GuardarItem` del mismo archivo.
- `OnClickEdit` en `GeoAgrLaborAuxiliarList.razor` (Tarea 11) navega a `/geoagrlaborauxiliaredicion` con los mismos query params (`compania`, `idEmpleado`, `fecha`, `idregistro`) que declara `GeoAgrLaborAuxiliarEdicion.razor` (Tarea 10) vía `[SupplyParameterFromQuery]`.
- `itemsAuxDet` en `SincronizacionManual.razor` (Tarea 12) filtra por `i.Item == itemDet.ITEM && i.IdEmpleadoAsignado == itemDet.IDNUMERO`, coherente con `IDNUMERO = d.IdEmpleadoAsignado!` en `GuardarEnGeotime` (Tarea 9, Step 2).

Si se detecta alguna inconsistencia, corríjala directamente en el archivo correspondiente y vuelva a ejecutar el build (Step 1).

- [ ] **Step 3: Prueba manual del flujo completo (opcional, requiere ejecutar la app)**

1. Crear una nueva Labor Auxiliar (`GeoAgrLaborAuxiliarDetalle.razor`, alta) con al menos 2 ítems y 2 empleados seleccionados → verificar que se guardan `2 × 2 = 4` filas en `GeoAgrLaborAuxiliarDet` (una por ítem × empleado).
2. Desde `GeoAgrLaborAuxiliarList.razor`, verificar que `CantDetalles` muestra 2 (ítems distintos) y `CantEmpleados` muestra 2 (empleados distintos).
3. Hacer clic en el registro → debe navegar a `/geoagrlaborauxiliaredicion` (no a `geoagrlaborauxiliardetalle`).
4. En `GeoAgrLaborAuxiliarEdicion.razor`, modificar la "Cantidad Asignada" de un solo empleado en un ítem y presionar "Guardar cambios del ítem N" → verificar que solo esa fila (`Item` + `IdEmpleadoAsignado`) cambió en SQLite, sin afectar a los demás empleados del mismo ítem.

No hay commit para esta tarea (es solo verificación final).

---
