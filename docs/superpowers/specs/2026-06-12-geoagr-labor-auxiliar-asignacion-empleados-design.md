# Spec: Asignación de cantidad por empleado en Labor Auxiliar

**Fecha:** 2026-06-12
**Proyecto:** LaboresNet (.NET MAUI Blazor)
**Alcance:** Cambiar el modelo local de `GeoAgrLaborAuxiliarDet` para que exista una fila por cada combinación ítem × empleado asignado, eliminar `GeoAgrLaborAuxiliarDetEmpAsig`, y crear un nuevo componente de edición que permita ajustar la cantidad asignada por colaborador sin afectar al resto del registro.

---

## 1. Contexto y motivación

Actualmente, al registrar una "Labor Auxiliar" (`GeoAgrLaborAuxiliarDetalle.razor`):
- Se capturan N ítems de detalle (`GeoAgrLaborAuxiliarDet`: bloque, finca, lote, ot, labor, recurso, cantidad de rendimiento, unidad).
- Se selecciona una lista de empleados (`GeoAgrLaborAuxiliarDetEmpAsig`), independiente de los ítems.
- Al sincronizar con Geotime, se hace un cruce ítem × empleado para generar `cSolicitudAuxLaborDet`.

Esto impide ajustar la cantidad asignada a **un solo colaborador** sin afectar a los demás, porque la asignación cantidad↔ítem no está vinculada a un empleado específico en el almacenamiento local.

**Cambio**: cada fila de `GeoAgrLaborAuxiliarDet` pasa a representar la asignación de UN ítem a UN empleado (`IdEmpleadoAsignado`), permitiendo editar la cantidad de cada colaborador de forma independiente.

`GeoAgrLaborAuxiliarDet` y `GeoAgrLaborAuxiliar` ya fueron movidos por el usuario desde `TicoFrutLibs` a `LaboresNet/Modelo/` como clases locales del proyecto (namespace `PortalEmpleadoGeotime.Client.Modelo`), por lo que pueden modificarse directamente sin afectar la librería externa.

---

## 2. Modelo de datos

### 2.1 `LaboresNet/Modelo/GeoAgrLaborAuxiliarDet.cs`

Se agrega:

```csharp
public string? IdEmpleadoAsignado { get; set; }
```

Sin atributo `[Column]` — no existe columna equivalente en Oracle, es exclusivo de la app local.

### 2.2 `GeoAgrLaborAuxiliarDetLocal` (`LocalEntities.cs`)

Se agrega la misma propiedad `IdEmpleadoAsignado`. La tabla SQLite cambia su PK (ver §3).

### 2.3 Eliminación de `GeoAgrLaborAuxiliarDetEmpAsig` / `GeoAgrLaborAuxiliarDetEmpAsigLocal`

Ambas clases se eliminan por completo de `LocalEntities.cs` (líneas 424-445 actuales).

---

## 3. Esquema SQLite y migración

### 3.1 Tabla `GeoAgrLaborAuxiliarDet`

Nueva definición ([MarcasDBLocal.cs:137-157](../../../LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs)):

```sql
CREATE TABLE IF NOT EXISTS GeoAgrLaborAuxiliarDet (
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
);
```

### 3.2 Tabla `GeoAgrLaborAuxiliarDetEmpAsig`

Se elimina el bloque `CREATE TABLE IF NOT EXISTS GeoAgrLaborAuxiliarDetEmpAsig` (líneas 160-170).

### 3.3 Migración (con pérdida de datos locales pendientes — aceptada)

Siguiendo el precedente ya comentado en el código (líneas 122-125), se agregan al inicio de la inicialización:

```csharp
database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDet").Wait();
database.ExecuteAsync("DROP TABLE IF EXISTS GeoAgrLaborAuxiliarDetEmpAsig").Wait();
```

Esto recrea ambas tablas con el nuevo esquema en el primer arranque tras actualizar la app, descartando registros locales no sincronizados aún. **Confirmado como aceptable por el usuario.**

---

## 4. Cambios en `MarcasDBLocal.cs`

### 4.1 `MapDetLocalToDet` (líneas 3426-3444)

Agrega `IdEmpleadoAsignado = l.IdEmpleadoAsignado`.

### 4.2 `SaveGeoAgrLaborAuxiliarDetAsync(IEnumerable<GeoAgrLaborAuxiliarDet>)` — upsert (líneas 3330-3410)

La clave de upsert pasa de `(Compania, IdEmpleado, Fecha, Item)` a `(Compania, IdEmpleado, Fecha, Item, IdEmpleadoAsignado)`. Para no romper llamadores que aún no setean `IdEmpleadoAsignado` (`Principal.razor:199`, sincronización TicoFrut), se aplica un fallback:

```csharp
var idEmpAsig = item.IdEmpleadoAsignado ?? item.IdEmpleado;
```

El `UPDATE` agrega `AND IdEmpleadoAsignado=?`; el `INSERT` setea `IdEmpleadoAsignado = idEmpAsig`.

### 4.3 `SaveGeoAgrLaborAuxiliarDetAsync(long idRegistro, IEnumerable<GeoAgrLaborAuxiliarDet>)` — insert puro (líneas 3694-3725)

El `GeoAgrLaborAuxiliarDetLocal` mapeado agrega `IdEmpleadoAsignado = item.IdEmpleadoAsignado`. Usado por el guardado de alta (§5).

### 4.4 Nuevo método: actualizar cantidad asignada por empleado

Usado por el botón "Guardar cambios del ítem" del nuevo componente de edición (§6):

```csharp
/// <summary>Actualiza la cantidad asignada (CantidadRendimiento) de cada empleado para un ítem específico.</summary>
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

Este guardado es **solo local** — no reenvía a Geotime.

### 4.5 Eliminación de la sección `GeoAgrLaborAuxiliarDetEmpAsig` (líneas 3483-3763)

Se elimina por completo: `MapLocalToDetEmpAsig`, las 4 sobrecargas de `GetGeoAgrLaborAuxiliarDetEmpAsigAsync`, las 2 de `SaveGeoAgrLaborAuxiliarDetEmpAsigAsync`, y las 4 de `DeleteGeoAgrLaborAuxiliarDetEmpAsigAsync` (incluida la basada en `idRegistro`, línea 3730).

`DeleteGeoAgrLaborAuxiliarDetAsync(long idRegistro)` (líneas 3682-3688, para `GeoAgrLaborAuxiliarDet` — distinta de la de EmpAsig) **se mantiene**.

---

## 5. `GeoAgrLaborAuxiliarDetalle.razor` → solo alta, expansión cartesiana

### 5.1 Simplificación a "solo nuevo"

- Se eliminan los `[SupplyParameterFromQuery]` `compania`, `idEmpleado`, `fecha`, `idregistro` (líneas 301-304).
- Se elimina la rama de edición completa en `OnInitializedAsync` (líneas 446-511): cargas de `GetGeoAgrLaborAuxiliarAsync`, `GetGeoAgrLaborAuxiliarDetAsync`, `GetGeoAgrLaborAuxiliarDetEmpAsigAsync`, `GetEstadoAsync`, pre-marcado de `empleadosSeleccionados` y población de `detalles` desde un registro existente. `OnInitializedAsync` conserva solo la rama "modo nuevo" (carga de catálogos + `encabezado.Compania`/`IdEmpleado` desde `loginState`).
- Se elimina el flag `esNuevo` (siempre alta) y el bloque `@if (!esNuevo)` de "Registro No." + badge de Estado (líneas 22-38).
- Se elimina `encabezado.Estado` de `EncabezadoForm`.

### 5.2 `GuardarDatos` (líneas 559-647) — expansión cartesiana

Siempre inserta encabezado nuevo (no hay "borrar hijos anteriores", no existen aún). `listaDets` se construye por **ítem × empleado seleccionado**, con la misma cantidad/datos para todos los empleados al momento del alta (ajustable después por empleado en `GeoAgrLaborAuxiliarEdicion`):

```csharp
idRegistroActual = await marcaDBLocal.InsertGeoAgrLaborAuxiliarAsync(header);

var listaDets = detalles
    .SelectMany(d => empleadosSeleccionados.Select(e => new GeoAgrLaborAuxiliarDet
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
        EstadoId            = 0,
        IdEmpleadoAsignado  = e.IdNumero,
    }))
    .ToList();

if (listaDets.Count > 0)
    await marcaDBLocal.SaveGeoAgrLaborAuxiliarDetAsync(idRegistroActual, listaDets);

await GuardarEnGeotime(header, listaDets, idRegistroActual);
```

La selección de empleados (`empleadosSeleccionados`, grid `RadzenDataGrid<EmpleadoExt>`) no cambia.

### 5.3 `GuardarEnGeotime` (líneas 649-711)

Ya no recibe `listaEmpAsig`. Mapea directo `listaDets` → `cSolicitudAuxLaborDet`, sin cruce, usando `IDNUMERO = d.IdEmpleadoAsignado!`:

```csharp
var listaDetalle = listaDets.Select(d => new cSolicitudAuxLaborDet
{
    SOLICITUDID = 0,
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
```

El resto (`cSolicitudAuxLabor`, `geoConnect.Post("SolicitudAuxLabor", ...)`, `SaveGeoAgrLaborAuxiliarEstadoAsync`) no cambia.

---

## 6. Nuevo componente `GeoAgrLaborAuxiliarEdicion.razor`

### 6.1 Ruta y navegación

- Ruta: `/geoagrlaborauxiliaredicion`, query params `compania`, `idEmpleado`, `fecha`, `idregistro` (los mismos que tenía el modo edición de `Detalle`).
- `GeoAgrLaborAuxiliarList.razor` → `OnClickEdit` (líneas 239-243): cambia el destino de `./geoagrlaborauxiliardetalle?...` a `./geoagrlaborauxiliaredicion?...`, mismos query params.
- `OnClickAdd` (líneas 234-237) no cambia — sigue navegando a `./geoagrlaborauxiliardetalle` (alta).

### 6.2 Carga de datos (`OnInitializedAsync`, en paralelo)

- `GetGeoAgrLaborAuxiliarAsync(compania, idEmpleado, fecha, idRegistro)` → encabezado (fecha, comentarios, estado general).
- `GetGeoAgrLaborAuxiliarDetAsync(compania, idEmpleado, fecha, idRegistro)` → una fila por ítem × empleado asignado.
- `GetEmpleadoAsync()` → diccionario `IdNumero → Nombre` (solo para mostrar nombre; no se vuelve a seleccionar empleados aquí).
- `GetEstadoAsync()` → `cEstado` para `EstadoId → Descripcion`.
- `GetGeoAgrOtAsync`, `GetGeoAgrOtLaborAsync`, `GetGeoAgrRecursoConceptoAsync` → resolver `NombreOt`/`NombreLabor`/`NombreRecurso` (igual patrón que `Detalle`).

### 6.3 Agrupación y view models

Los registros se agrupan por `Item` (`GroupBy(d => d.Item)`), un "card" por ítem:

```csharp
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
```

### 6.4 Layout (ver mockup `geoagr_labor_auxiliar_edicion_mockup`)

- Header card: número de labor, fecha, empleado responsable, badge de estado general.
- Por cada `GeoAgrLaborAuxiliarItemVM`: card con acento verde TicoFrut, grid 2 columnas de datos generales de solo lectura (Bloque/Finca, Lote, OT, Labor, Recurso, Unidad), seguido de filas por empleado:
  - Avatar + nombre + `IdEmpleadoAsignado`.
  - **"Asignada"** (`CantidadAsignada`) → input numérico editable (RadzenNumeric inline).
  - **"Aprobada"** (`CantidadAprobada`) → solo lectura.
  - **"Estado"** (`EstadoDescripcion`) → badge informativo de solo lectura.
  - Botón "Guardar cambios del ítem N" al final de cada card.

### 6.5 Guardado por ítem — solo local

```csharp
private async Task GuardarItem(GeoAgrLaborAuxiliarItemVM itemVM)
{
    var cantidades = itemVM.Empleados.Select(e => (e.IdEmpleadoAsignado, e.CantidadAsignada));
    await marcaDBLocal.UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync(idRegistroActual, itemVM.Item, cantidades);
    await swal.FireAsync("Atención", "Cambios guardados", SweetAlertIcon.Success);
}
```

No reenvía a Geotime — `Estado` y `Cantidad Aprobada` reflejan datos que llegan vía sincronización y no se modifican desde aquí.

---

## 7. `GeoAgrLaborAuxiliarList.razor`

### 7.1 `CantEmpleados` (líneas 187-232)

Ya no se llama a `GetGeoAgrLaborAuxiliarDetEmpAsigAsync` (eliminado). Se recalcula contando `IdEmpleadoAsignado` distintos sobre `detalles` (ya cargado en línea 208):

```csharp
CantEmpleados = detalles?.Select(d => d.IdEmpleadoAsignado).Distinct().Count() ?? 0,
```

### 7.2 `CantDetalles` (corrección por consistencia)

Con la expansión cartesiana, `detalles` ahora contiene una fila por ítem × empleado, por lo que `detalles?.Count()` ya no representa la cantidad de ítems/actividades. Se recalcula contando `Item` distintos:

```csharp
CantDetalles = detalles?.Select(d => d.Item).Distinct().Count() ?? 0,
```

---

## 8. `SincronizacionManual.razor`

Bloque de actualización de estado tras aprobación en Geotime (líneas 220-247):

- Con la expansión cartesiana, `itemsLaborAuxiliar` (= `GetGeoAgrLaborAuxiliarDetAsync`) ahora tiene **una fila por ítem × empleado asignado**. `cSolicitudAuxLaborDet` (cada `itemDet`) también es por ítem × empleado (`ITEM` + `IDNUMERO`, ver §5.3). Por lo tanto, el filtro de `itemsAuxDet` debe incluir ambos campos para actualizar solo la fila del empleado correspondiente, no todos los empleados asignados a ese ítem:

  ```csharp
  var itemsAuxDet = itemsLaborAuxiliar
      .Where(i => i.IdRegistro == solicitud.IDREGISTROAPP
                && i.Item == itemDet.ITEM
                && i.IdEmpleadoAsignado == itemDet.IDNUMERO)
      .ToList();
  ```

  Luego, `SaveGeoAgrLaborAuxiliarDetAsync(itemsAuxDetActualizar)` (upsert, §4.2) actualiza el estado de esa fila empleado×ítem correctamente, usando `IdEmpleadoAsignado` ya poblado por `MapDetLocalToDet` (§4.1).
- El bloque que actualizaba `itemsAuxDetEmp` vía `GetGeoAgrLaborAuxiliarDetEmpAsigAsync`/`SaveGeoAgrLaborAuxiliarDetEmpAsigAsync` (líneas 238-245) **se elimina** — ya no aplica, queda cubierto por el punto anterior.

---

## 9. Resumen de archivos afectados

| Archivo | Cambio |
|---|---|
| `LaboresNet/Modelo/GeoAgrLaborAuxiliarDet.cs` | Agregar `IdEmpleadoAsignado` |
| `LaboresNet/Data/DataBaseLocal/LocalEntities.cs` | Agregar `IdEmpleadoAsignado` a `GeoAgrLaborAuxiliarDetLocal`; eliminar `GeoAgrLaborAuxiliarDetEmpAsig`/`GeoAgrLaborAuxiliarDetEmpAsigLocal` |
| `LaboresNet/Data/DataBaseLocal/MarcasDBLocal.cs` | Nueva PK + DROP de migración; `MapDetLocalToDet`; ambos `SaveGeoAgrLaborAuxiliarDetAsync`; nuevo `UpdateGeoAgrLaborAuxiliarDetCantidadAsignadaAsync`; eliminar sección EmpAsig completa |
| `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor` | Solo alta: eliminar modo edición, query params, `esNuevo`, badge Estado; `GuardarDatos`/`GuardarEnGeotime` con expansión cartesiana |
| `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarEdicion.razor` | **Nuevo** — edición de cantidad asignada por empleado |
| `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor` | `OnClickEdit` → nueva ruta; `CantEmpleados`/`CantDetalles` recalculados |
| `LaboresNet/Components/Pages/Procesos/SincronizacionManual.razor` | Eliminar bloque de actualización vía EmpAsig |

---

## 10. Mockup de referencia

Mockup `geoagr_labor_auxiliar_edicion_mockup` ya mostrado y aprobado (con la corrección: **"Asignada" es el campo editable**, "Aprobada" y "Estado" son de solo lectura).
