# Migración Blazor.Bootstrap → Radzen — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminar todos los controles Blazor.Bootstrap del proyecto LaboresNet (.NET MAUI Blazor) y sustituirlos por equivalentes Radzen Blazor o HTML Bootstrap nativo. SweetAlert2 permanece sin cambios.

**Architecture:** Migración tipo-por-tipo en 7 fases: Spinner/Preload → Icons → Accordion → Inputs especializados → Switch/Button/Card/Tabs → Grid (patrón DataProvider→Data binding directo) → Limpieza de imports.

**Tech Stack:** .NET MAUI Blazor, Radzen Blazor 5.x, Bootstrap 5 CSS nativo, Material Icons (via Radzen)

---

## Archivos modificados por fase

| Fase | Archivos |
|---|---|
| 1 — Spinner/Preload | `SpinnerControl.razor`, `Home.razor` |
| 2 — Icons | `PageReturn.razor`, `PageMant.razor`, `Principal.razor`, `EmpleadoDetalle.razor`, `EmpleadoExtList.razor`, `EmpleadoMarcaList.razor`, `EmpleadoJefaturaList.razor`, `EmpleadoJefaturaDetalle.razor`, `EmpleadoGrupoModal.razor`, `GeoAgrLaborAuxiliarDetalle.razor`, `GeoAgrLaborAuxiliarList.razor`, `Marcas.razor`, `MarcasLocalizacion.razor`, `MarcasDistribucionDetalle.razor`, `MarcasDistribucionList.razor`, `EnrolarEmpleado.razor`, `RegistroMarcas.razor` |
| 3 — Accordion | `Marcas.razor`, `EmpleadoMarcaList.razor`, `MarcasLocalizacion.razor`, `MarcasDistribucionDetalle.razor`, `MarcasDistribucionList.razor`, `GeoAgrLaborAuxiliarDetalle.razor`, `GeoAgrLaborAuxiliarList.razor`, `RegistroMarcas.razor` |
| 4 — DateInput / NumberInput | `GeoAgrLaborAuxiliarDetalle.razor`, `GeoAgrLaborAuxiliarList.razor`, `MarcasDistribucionDetalle.razor` |
| 5 — Switch / Button / Card / Tabs | `EmpleadoDetalle.razor`, `RegistroMarcas.razor`, `EmpleadoGrupoModal.razor`, `EmpleadoJefaturaList.razor` |
| 6 — Grid | `GeoAgrFincaPop.razor`, `GeoAgrLotePop.razor`, `GeoAgrOtPop.razor`, `GeoAgrOtLaborPop.razor`, `CentroCostoPop.razor`, `ConceptosPop.razor`, `ProyectosPop.razor`, `GeoAgrRecursoConceptoPop.razor`, `UsuarioPop.razor`, `IconsPop.razor`, `EmpleadoList.razor`, `EmpleadoExtList.razor`, `MarcasLocalizacion.razor`, `EmpleadoJefaturaList.razor`, `EmpleadoGrupoModal.razor` |
| 7 — Limpieza | `_Imports.razor` |

---

## Tabla de mapeo de iconos (referencia global)

| `IconName.X` | Material Icon |
|---|---|
| `ArrowLeft` | `arrow_back` |
| `ArrowRight` | `arrow_forward` |
| `ArrowUpLeftCircle` | `north_west` |
| `Calendar2DateFill` | `event` |
| `Check2Circle` | `check_circle` |
| `Clock` | `schedule` |
| `ClockHistory` | `history` |
| `Filter` | `filter_alt` |
| `LayoutTextWindow` | `dashboard` |
| `ListNested` | `format_list_bulleted` |
| `Markdown` | `notes` |
| `Person` | `person` |
| `PersonBoundingBox` | `face` |
| `PersonCheck` | `how_to_reg` |
| `PeopleFill` | `group` |
| `PinMap` | `location_on` |
| `Plus` | `add` |
| `Save` | `save` |
| `Search` | `search` |
| `Trash` | `delete` |
| `Upload` | `upload` |
| `XCircleFill` | `cancel` |
| `ZoomIn` | `zoom_in` |
| `(IconName)551` | `fingerprint` |
| `(IconName)1818` | `groups` |
| `(IconName)1455` | `face` |
| `(IconName)1303` | `agriculture` |

Atributos de tamaño/color de `<Icon>`:
- `Size="IconSize.x2"` → `Style="font-size:1.5rem;"` en RadzenIcon
- `Size="IconSize.x5"` → `Style="font-size:2rem;"` en RadzenIcon
- `Color="IconColor.Success"` → `Style="color:var(--rz-success);"` en RadzenIcon
- `Color="IconColor.Danger"` → `Style="color:var(--rz-danger);"` en RadzenIcon
- `Class="me-1"` → mantener como atributo en RadzenIcon

---

## FASE 1 — Spinner y Preload

### Tarea 1: SpinnerControl.razor — reemplazar `<Spinner>` con spinner Bootstrap nativo

**Archivos:**
- Modificar: `LaboresNet/Components/Layout/SpinnerControl.razor`

- [ ] **Reemplazar contenido completo del archivo**

```razor
<div class="text-center">
    <div class="spinner-border text-warning" role="status" style="width:3rem;height:3rem;">
        <span class="visually-hidden">Cargando...</span>
    </div>
    <br />Cargando...
</div>
```

- [ ] **Commit**
```
git add LaboresNet/Components/Layout/SpinnerControl.razor
git commit -m "feat: reemplazar Spinner BB por spinner Bootstrap nativo"
```

---

### Tarea 2: Home.razor — eliminar `<Preload>` y `PreloadService`

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Home.razor`

- [ ] **Eliminar la línea `<Preload LoadingText="Iniciando sesión en portal..." />`** (línea 96)

- [ ] **Eliminar la línea con `[Inject] protected PreloadService PreloadService { get; set; } = default!;`** (línea 99 del `@code`)

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Home.razor
git commit -m "feat: eliminar Preload BB de Home.razor"
```

---

## FASE 2 — Iconos (`<Icon Name="IconName.X">` → `<RadzenIcon Icon="material_name"/>`)

### Tarea 3: PageReturn.razor y PageMant.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Layout/PageReturn.razor`
- Modificar: `LaboresNet/Components/Layout/PageMant.razor`

- [ ] **PageReturn.razor** — reemplazar `<Icon Name="IconName.ArrowLeft" />`

```razor
<RadzenIcon Icon="arrow_back" />
```

- [ ] **PageMant.razor** — reemplazar ambos iconos

```razor
@* ArrowLeft *@
<RadzenIcon Icon="arrow_back" />

@* Plus *@
<RadzenIcon Icon="add" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Layout/PageReturn.razor LaboresNet/Components/Layout/PageMant.razor
git commit -m "feat: reemplazar Icon BB en PageReturn y PageMant"
```

---

### Tarea 4: Principal.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Principal.razor`

- [ ] **Reemplazar los 5 `<Icon>` con iconos numéricos y ArrowUpLeftCircle**

```razor
@* (IconName)551 — Registro de Marcas *@
<RadzenIcon Icon="fingerprint" Style="font-size: 3rem; margin-bottom: 1rem; color: var(--primary-color)" />

@* (IconName)1818 — Registro de Marcas Grupal *@
<RadzenIcon Icon="groups" Style="font-size: 3rem; margin-bottom: 1rem; color: var(--primary-color)" />

@* (IconName)1455 — Registro de Rostros *@
<RadzenIcon Icon="face" Style="font-size: 3rem; margin-bottom: 1rem; color: var(--primary-color)" />

@* (IconName)1303 — Labores Auxiliares *@
<RadzenIcon Icon="agriculture" Style="font-size: 3rem; margin-bottom: 1rem; color: var(--primary-color)" />

@* IconName.ArrowUpLeftCircle — Sincronizar *@
<RadzenIcon Icon="north_west" Style="font-size: 3rem; margin-bottom: 1rem; color: var(--primary-color)" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Principal.razor
git commit -m "feat: reemplazar Icon BB en Principal.razor"
```

---

### Tarea 5: EmpleadoDetalle.razor (iconos)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/EmpleadoDetalle.razor`

- [ ] **Reemplazar `<Icon Name="IconName.Trash" />`** (botón Eliminar)
```razor
<RadzenIcon Icon="delete" />
```

- [ ] **Reemplazar `<Icon Name="IconName.Save" />`** (botón Aceptar)
```razor
<RadzenIcon Icon="save" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/EmpleadoDetalle.razor
git commit -m "feat: reemplazar Icon BB en EmpleadoDetalle.razor"
```

---

### Tarea 6: EmpleadoExtList.razor (iconos)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/EmpleadoExtList.razor`

- [ ] **Reemplazar los 3 iconos en la columna "Enrolado"**

```razor
@* PersonBoundingBox con Size x2 Color Success *@
<RadzenIcon Icon="face" Style="font-size:1.5rem;color:var(--rz-success);" />

@* Trash con Size x2 Color Danger + onclick *@
<RadzenIcon Icon="delete" Style="font-size:1.5rem;color:var(--rz-danger);cursor:pointer;" @onclick="(e) => EliminarRostros(context)" />

@* XCircleFill con Size x2 Color Danger *@
<RadzenIcon Icon="cancel" Style="font-size:1.5rem;color:var(--rz-danger);" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/EmpleadoExtList.razor
git commit -m "feat: reemplazar Icon BB en EmpleadoExtList.razor"
```

---

### Tarea 7: EmpleadoMarcaList.razor, EnrolarEmpleado.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/EmpleadoMarcaList.razor`
- Modificar: `LaboresNet/Components/Pages/Procesos/EnrolarEmpleado.razor`

- [ ] **EmpleadoMarcaList.razor — reemplazar 2 iconos**

```razor
@* IconName.PersonCheck *@
<RadzenIcon Icon="how_to_reg" class="me-1" />

@* IconName.PinMap *@
<RadzenIcon Icon="location_on" class="me-1" />
```

- [ ] **EnrolarEmpleado.razor — reemplazar 1 icono**

```razor
@* IconName.Person Size x1 Color Success *@
<RadzenIcon Icon="person" Style="color:var(--rz-success);" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/EmpleadoMarcaList.razor
git add LaboresNet/Components/Pages/Procesos/EnrolarEmpleado.razor
git commit -m "feat: reemplazar Icon BB en EmpleadoMarcaList y EnrolarEmpleado"
```

---

### Tarea 8: EmpleadoJefaturaList.razor, EmpleadoJefaturaDetalle.razor, EmpleadoGrupoModal.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Configuracion/EmpleadoJefaturaList.razor`
- Modificar: `LaboresNet/Components/Pages/Configuracion/EmpleadoJefaturaDetalle.razor`
- Modificar: `LaboresNet/Components/Pages/Configuracion/EmpleadoGrupoModal.razor`

- [ ] **EmpleadoJefaturaList.razor — reemplazar 2 iconos en TitleTemplate de Tab**

```razor
@* IconName.LayoutTextWindow *@
<RadzenIcon Icon="dashboard" />

@* IconName.Upload *@
<RadzenIcon Icon="upload" />
```

- [ ] **EmpleadoJefaturaDetalle.razor — reemplazar 2 iconos Search con Size x5**

```razor
<RadzenIcon Icon="search" Style="font-size:2rem;" />
```
(aplicar en ambas ocurrencias)

- [ ] **EmpleadoGrupoModal.razor — reemplazar 2 iconos ArrowRight/ArrowLeft con Size x5**

```razor
@* ArrowRight *@
<RadzenIcon Icon="arrow_forward" Style="font-size:2rem;" />

@* ArrowLeft *@
<RadzenIcon Icon="arrow_back" Style="font-size:2rem;" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Configuracion/EmpleadoJefaturaList.razor
git add LaboresNet/Components/Pages/Configuracion/EmpleadoJefaturaDetalle.razor
git add LaboresNet/Components/Pages/Configuracion/EmpleadoGrupoModal.razor
git commit -m "feat: reemplazar Icon BB en componentes de Configuracion"
```

---

### Tarea 9: GeoAgrLaborAuxiliarDetalle.razor, GeoAgrLaborAuxiliarList.razor (iconos)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`
- Modificar: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor`

- [ ] **GeoAgrLaborAuxiliarDetalle.razor — reemplazar 9 iconos**

| Ubicación | Antes | Después |
|---|---|---|
| AccordionItem "Datos de la Labor" | `<Icon Name="IconName.Calendar2DateFill" Class="me-1" />` | `<RadzenIcon Icon="event" class="me-1" />` |
| AccordionItem "Colaboradores" | `<Icon Name="IconName.PeopleFill" Class="me-1" />` | `<RadzenIcon Icon="group" class="me-1" />` |
| AccordionItem "Líneas de Detalle" | `<Icon Name="IconName.ListNested" Class="me-1" />` | `<RadzenIcon Icon="format_list_bulleted" class="me-1" />` |
| Botón "Agregar línea" | `<Icon Name="IconName.Plus" />` | `<RadzenIcon Icon="add" />` |
| Botón eliminar ítem (×3) | `<Icon Name="IconName.Trash" />` | `<RadzenIcon Icon="delete" />` |
| Botón lupa Finca | `<Icon Name="IconName.ZoomIn" />` | `<RadzenIcon Icon="zoom_in" />` |
| Botón lupa Lote | `<Icon Name="IconName.ZoomIn" />` | `<RadzenIcon Icon="zoom_in" />` |
| Botón lupa OT/Labor | `<Icon Name="IconName.ZoomIn" />` | `<RadzenIcon Icon="zoom_in" />` |
| Botón Guardar | `<Icon Name="IconName.Save" />` | `<RadzenIcon Icon="save" />` |

- [ ] **GeoAgrLaborAuxiliarList.razor — reemplazar 2 iconos**

```razor
@* Filter *@
<RadzenIcon Icon="filter_alt" class="me-1" />

@* ListNested *@
<RadzenIcon Icon="format_list_bulleted" class="me-1" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor
git commit -m "feat: reemplazar Icon BB en GeoAgrLaborAuxiliar*.razor"
```

---

### Tarea 10: Marcas.razor, MarcasLocalizacion.razor, MarcasDistribucionDetalle.razor, MarcasDistribucionList.razor, RegistroMarcas.razor (iconos)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/Marcas.razor`
- Modificar: `LaboresNet/Components/Pages/Procesos/MarcasLocalizacion.razor`
- Modificar: `LaboresNet/Components/Pages/Procesos/MarcasDistribucionDetalle.razor`
- Modificar: `LaboresNet/Components/Pages/Procesos/MarcasDistribucionList.razor`
- Modificar: `LaboresNet/Components/Pages/RelojMarcador/RegistroMarcas.razor`

- [ ] **Marcas.razor — reemplazar 3 iconos**

```razor
@* Check2Circle (en span con onclick) *@
<RadzenIcon Icon="check_circle" class="me-1" />

@* PinMap *@
<RadzenIcon Icon="location_on" class="me-1" />

@* Markdown *@
<RadzenIcon Icon="notes" class="me-1" />
```

- [ ] **MarcasLocalizacion.razor — reemplazar 4 iconos**

```razor
@* ArrowLeft (en <a> tag) *@
<RadzenIcon Icon="arrow_back" />

@* Filter *@
<RadzenIcon Icon="filter_alt" class="me-1" />

@* PinMap *@
<RadzenIcon Icon="location_on" class="me-1" />

@* Markdown *@
<RadzenIcon Icon="notes" class="me-1" />
```

- [ ] **MarcasDistribucionDetalle.razor — reemplazar 5 iconos**

```razor
@* Calendar2DateFill *@
<RadzenIcon Icon="event" class="me-1" />

@* ZoomIn (Centro Costo) *@
<RadzenIcon Icon="zoom_in" />

@* ZoomIn (Proyecto) *@
<RadzenIcon Icon="zoom_in" />

@* PeopleFill *@
<RadzenIcon Icon="group" class="me-1" />

@* Save *@
<RadzenIcon Icon="save" />
```

- [ ] **MarcasDistribucionList.razor — reemplazar 2 iconos**

```razor
@* Filter *@
<RadzenIcon Icon="filter_alt" class="me-1" />

@* ListNested *@
<RadzenIcon Icon="format_list_bulleted" class="me-1" />
```

- [ ] **RegistroMarcas.razor — reemplazar 2 iconos**

```razor
@* Person (en span con onclick) *@
<RadzenIcon Icon="person" class="me-1" />

@* ClockHistory *@
<RadzenIcon Icon="history" class="me-1" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/Marcas.razor
git add LaboresNet/Components/Pages/Procesos/MarcasLocalizacion.razor
git add LaboresNet/Components/Pages/Procesos/MarcasDistribucionDetalle.razor
git add LaboresNet/Components/Pages/Procesos/MarcasDistribucionList.razor
git add LaboresNet/Components/Pages/RelojMarcador/RegistroMarcas.razor
git commit -m "feat: reemplazar Icon BB en componentes de Marcas"
```

---

## FASE 3 — Accordion → RadzenPanel

**Patrón general:**
```razor
@* ANTES *@
<Accordion AlwaysOpen="true">
    <AccordionItem Active="true">
        <TitleTemplate>Título con icono</TitleTemplate>
        <Content>Contenido</Content>
    </AccordionItem>
</Accordion>

@* DESPUÉS *@
<div>
    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>Título con icono</HeaderTemplate>
        <ChildContent>Contenido</ChildContent>
    </RadzenPanel>
</div>
```

Notas:
- El wrapper `<Accordion>` se reemplaza con un `<div>`. Si tenía `Class="ibm-plex-serif-bold"`, añadir a `class` del div.
- `AccordionItem Active="true"` → `RadzenPanel AllowCollapse="true"` (expandido por defecto)
- `AccordionItem` sin `Active` → `RadzenPanel AllowCollapse="true" Collapsed="true"` (colapsado)

---

### Tarea 11: GeoAgrLaborAuxiliarDetalle.razor, GeoAgrLaborAuxiliarList.razor (Accordion)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`
- Modificar: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor`

- [ ] **GeoAgrLaborAuxiliarDetalle.razor** — 3 AccordionItems

```razor
@* Reemplazar <Accordion AlwaysOpen="true"> con <div> *@
<div>
    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="event" class="me-1" /> Datos de la Labor
        </HeaderTemplate>
        <ChildContent>
            @* contenido sin cambios *@
        </ChildContent>
    </RadzenPanel>

    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="group" class="me-1" /> Colaboradores asignados
        </HeaderTemplate>
        <ChildContent>
            @* contenido sin cambios *@
        </ChildContent>
    </RadzenPanel>

    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="format_list_bulleted" class="me-1" /> Líneas de Detalle
        </HeaderTemplate>
        <ChildContent>
            @* contenido sin cambios *@
        </ChildContent>
    </RadzenPanel>
</div>
```

- [ ] **GeoAgrLaborAuxiliarList.razor** — 2 AccordionItems

```razor
<div>
    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="filter_alt" class="me-1" /> Filtrar por:
        </HeaderTemplate>
        <ChildContent>
            @* EditForm con filtros sin cambios *@
        </ChildContent>
    </RadzenPanel>

    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="format_list_bulleted" class="me-1" /> Labores Auxiliares
        </HeaderTemplate>
        <ChildContent>
            @* RadzenDataGrid sin cambios *@
        </ChildContent>
    </RadzenPanel>
</div>
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor
git commit -m "feat: reemplazar Accordion BB en GeoAgrLaborAuxiliar*.razor"
```

---

### Tarea 12: Marcas.razor, EmpleadoMarcaList.razor (Accordion)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/Marcas.razor`
- Modificar: `LaboresNet/Components/Pages/Procesos/EmpleadoMarcaList.razor`

- [ ] **Marcas.razor** — 3 AccordionItems (el 2do es condicional)

```razor
<div class="ibm-plex-serif-bold">
    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <span @onclick="VerFaceLogs" @onclick:stopPropagation="true" style="cursor:pointer;">
                <RadzenIcon Icon="check_circle" class="me-1" />
            </span> Registrar Marcas
        </HeaderTemplate>
        <ChildContent>
            @* contenido sin cambios *@
        </ChildContent>
    </RadzenPanel>

    @if (portal_Config!.USARGEOLOCALIZACION && mapsInitialized && latitud > 0)
    {
        <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
            <HeaderTemplate>
                <RadzenIcon Icon="location_on" class="me-1" /> Ubicación
            </HeaderTemplate>
            <ChildContent>
                <div id="googleMapContainer" style="width:100%;height:400px"></div>
            </ChildContent>
        </RadzenPanel>
    }

    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="notes" class="me-1" /> Marcas registradas
        </HeaderTemplate>
        <ChildContent>
            @* RadzenDataGrid sin cambios *@
        </ChildContent>
    </RadzenPanel>
</div>
```

- [ ] **EmpleadoMarcaList.razor** — 2 AccordionItems (el 2do es condicional)

```razor
<div class="ibm-plex-serif-bold">
    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="how_to_reg" class="me-1" /> Lista de Empleados Asignados
        </HeaderTemplate>
        <ChildContent>
            @* RadzenDataGrid sin cambios *@
        </ChildContent>
    </RadzenPanel>

    @if (portal_Config!.USARGEOLOCALIZACION && mapsInitialized && latitud > 0)
    {
        <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
            <HeaderTemplate>
                <RadzenIcon Icon="location_on" class="me-1" /> Ubicación
            </HeaderTemplate>
            <ChildContent>
                <div id="googleMapContainer" style="width:100%;height:400px"></div>
            </ChildContent>
        </RadzenPanel>
    }
</div>
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/Marcas.razor
git add LaboresNet/Components/Pages/Procesos/EmpleadoMarcaList.razor
git commit -m "feat: reemplazar Accordion BB en Marcas y EmpleadoMarcaList"
```

---

### Tarea 13: MarcasDistribucionDetalle.razor, MarcasDistribucionList.razor (Accordion)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/MarcasDistribucionDetalle.razor`
- Modificar: `LaboresNet/Components/Pages/Procesos/MarcasDistribucionList.razor`

- [ ] **MarcasDistribucionDetalle.razor** — 2 AccordionItems

```razor
<div>
    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="event" class="me-1" /> Ingrese datos para la distribución
        </HeaderTemplate>
        <ChildContent>
            @* contenido existente sin cambios *@
        </ChildContent>
    </RadzenPanel>

    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="group" class="me-1" /> Colaboradores asignados
        </HeaderTemplate>
        <ChildContent>
            @* RadzenDataGrid sin cambios *@
        </ChildContent>
    </RadzenPanel>
</div>
```

- [ ] **MarcasDistribucionList.razor** — 2 AccordionItems (el 1ro es condicional)

```razor
<div>
    @if (sincronizar)
    {
        <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
            <HeaderTemplate>
                <RadzenIcon Icon="filter_alt" class="me-1" /> Filtrar por:
            </HeaderTemplate>
            <ChildContent>
                @* EditForm sin cambios *@
            </ChildContent>
        </RadzenPanel>
    }

    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="format_list_bulleted" class="me-1" /> Distribución de Tiempo
        </HeaderTemplate>
        <ChildContent>
            @* RadzenDataGrid sin cambios *@
        </ChildContent>
    </RadzenPanel>
</div>
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/MarcasDistribucionDetalle.razor
git add LaboresNet/Components/Pages/Procesos/MarcasDistribucionList.razor
git commit -m "feat: reemplazar Accordion BB en MarcasDistribucion*.razor"
```

---

### Tarea 14: MarcasLocalizacion.razor (Accordion)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/MarcasLocalizacion.razor`

- [ ] **MarcasLocalizacion.razor** — 3 AccordionItems (el 1ro SIN Active → Collapsed, otros con Active)

```razor
<div>
    @* Sin Active="true" → Collapsed="true" *@
    <RadzenPanel AllowCollapse="true" Collapsed="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="filter_alt" class="me-1" /> Filtrar por:
        </HeaderTemplate>
        <ChildContent>
            @* EditForm sin cambios *@
        </ChildContent>
    </RadzenPanel>

    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="location_on" class="me-1" /> Marcas Registradas
        </HeaderTemplate>
        <ChildContent>
            @* mapa sin cambios *@
        </ChildContent>
    </RadzenPanel>

    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <RadzenIcon Icon="notes" class="me-1" /> Detalle
        </HeaderTemplate>
        <ChildContent>
            @* Grid — se migrará en Fase 6 *@
        </ChildContent>
    </RadzenPanel>
</div>
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/MarcasLocalizacion.razor
git commit -m "feat: reemplazar Accordion BB en MarcasLocalizacion.razor"
```

---

### Tarea 15: RegistroMarcas.razor (Accordion)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/RelojMarcador/RegistroMarcas.razor`

- [ ] **RegistroMarcas.razor** — 2 AccordionItems (el 2do es condicional)

```razor
<div class="ibm-plex-serif-bold">
    <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
        <HeaderTemplate>
            <span @onclick="VerFaceLogs" @onclick:stopPropagation="true" style="cursor:pointer;">
                <RadzenIcon Icon="person" class="me-1" />
            </span> Por favor seleccione el tipo de marca y acerque su rostro a la pantalla
        </HeaderTemplate>
        <ChildContent>
            @* contenido sin cambios *@
        </ChildContent>
    </RadzenPanel>

    @if (portal_Config!.VERLOGMARCAS)
    {
        <RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
            <HeaderTemplate>
                <RadzenIcon Icon="history" class="me-1" /> Historial de marca
            </HeaderTemplate>
            <ChildContent>
                @* RadzenListBox sin cambios *@
            </ChildContent>
        </RadzenPanel>
    }
</div>
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/RelojMarcador/RegistroMarcas.razor
git commit -m "feat: reemplazar Accordion BB en RegistroMarcas.razor"
```

---

## FASE 4 — DateInput y NumberInput

### Tarea 16: GeoAgrLaborAuxiliarDetalle.razor (DateInput + NumberInput)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`

- [ ] **Reemplazar `<DateInput TValue="DateOnly" EnableMinMax="true" Min="@fechaMin" Max="@fechaHoy" ...>`**

```razor
<RadzenDatePicker TValue="DateOnly"
                 @bind-Value="encabezado.FechaDO"
                 Min="@fechaMin.ToDateTime(TimeOnly.MinValue)"
                 Max="@fechaHoy.ToDateTime(TimeOnly.MaxValue)"
                 DateFormat="dd/MM/yyyy"
                 class="form-control br8 rounded-border"
                 Style="width:100%;" />
```

- [ ] **Reemplazar `<NumberInput TValue="decimal?" ... @bind-Value="det.CantidadRendimiento" />`** (3 ocurrencias en los items de detalle)

```razor
<RadzenNumeric TValue="decimal?"
              @bind-Value="det.CantidadRendimiento"
              Placeholder="0.00"
              Format="n2"
              class="form-control br8 rounded-border"
              Style="width:100%;" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor
git commit -m "feat: reemplazar DateInput/NumberInput BB en GeoAgrLaborAuxiliarDetalle"
```

---

### Tarea 17: GeoAgrLaborAuxiliarList.razor (DateInput ×2)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor`

- [ ] **Reemplazar `<DateInput TValue="DateOnly" ... @bind-Value="parametros.FechaDesde" />`**

```razor
<RadzenDatePicker TValue="DateOnly"
                 @bind-Value="parametros.FechaDesde"
                 DateFormat="dd/MM/yyyy"
                 class="form-control br8 rounded-border"
                 Style="width:100%; font-size:small;" />
```

- [ ] **Reemplazar `<DateInput TValue="DateOnly" ... @bind-Value="parametros.FechaHasta" />`**

```razor
<RadzenDatePicker TValue="DateOnly"
                 @bind-Value="parametros.FechaHasta"
                 DateFormat="dd/MM/yyyy"
                 class="form-control br8 rounded-border"
                 Style="width:100%; font-size:small;" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/GeoAgrLaborAuxiliarList.razor
git commit -m "feat: reemplazar DateInput BB en GeoAgrLaborAuxiliarList"
```

---

### Tarea 18: MarcasDistribucionDetalle.razor (DateInput)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/MarcasDistribucionDetalle.razor`

- [ ] **Reemplazar `<DateInput TValue="DateOnly" EnableMinMax="true" Min="@fechaMin" Max="@fecha" @bind-Value="marcaDistribucion.FechaDO" />`**

```razor
<RadzenDatePicker TValue="DateOnly"
                 @bind-Value="marcaDistribucion.FechaDO"
                 Min="@fechaMin.ToDateTime(TimeOnly.MinValue)"
                 Max="@fecha.ToDateTime(TimeOnly.MaxValue)"
                 DateFormat="dd/MM/yyyy"
                 class="form-control br8 rounded-border"
                 Style="width:100%;" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/MarcasDistribucionDetalle.razor
git commit -m "feat: reemplazar DateInput BB en MarcasDistribucionDetalle"
```

---

## FASE 5 — Switch, Button, Card, Tabs

### Tarea 19: EmpleadoDetalle.razor — Switch → RadzenCheckBox

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/EmpleadoDetalle.razor`

- [ ] **Reemplazar `<Switch Class="form-check-input-blue" @bind-Value="Habilitado" ElementId="estado" />`**

```razor
<RadzenCheckBox @bind-Value="Habilitado" Name="estado" />
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/EmpleadoDetalle.razor
git commit -m "feat: reemplazar Switch BB por RadzenCheckBox en EmpleadoDetalle"
```

---

### Tarea 20: RegistroMarcas.razor — Button BB → button HTML

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/RelojMarcador/RegistroMarcas.razor`

- [ ] **Reemplazar los 3 `<Button class="nav-lnk" @onclick="...">` (capital B = BB)**

```razor
@* Cambiar <Button ...> por <button ...> (minúscula = HTML nativo) *@
<button class="nav-lnk" @onclick="() => RegistrarMarca(1)">
    @* SVG sin cambios *@
    <span style="font-size:@fontSize;color:green">Entrada</span>
</button>

<button class="nav-lnk" @onclick="() => RegistrarMarca(3)">
    @* SVG sin cambios *@
    <span style="font-size:@fontSize;color:green">Descanso</span>
</button>

<button class="nav-lnk" @onclick="() => RegistrarMarca(2)">
    @* SVG sin cambios *@
    <span style="font-size:@fontSize;color:green">Salida</span>
</button>
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/RelojMarcador/RegistroMarcas.razor
git commit -m "feat: reemplazar Button BB por button HTML en RegistroMarcas"
```

---

### Tarea 21: EmpleadoGrupoModal.razor — Button, Card → HTML Bootstrap

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Configuracion/EmpleadoGrupoModal.razor`

- [ ] **Reemplazar `<Card>/<CardBody>/<CardHeader>/<CardTitle>` con `<div>` Bootstrap**

```razor
@* ANTES *@
<Card>
    <CardBody>
        <div class="container-fluid">
            ...
            <Card Color="CardColor.Light">
                <CardHeader>Empleados Compañia</CardHeader>
                <CardBody>
                    <CardTitle>
                        <Button Color="ButtonColor.Success" ...>...</Button>
                    </CardTitle>
                    <Grid ...>...</Grid>
                </CardBody>
            </Card>
            ...
        </div>
    </CardBody>
</Card>

@* DESPUÉS *@
<div class="card">
    <div class="card-body">
        <div class="container-fluid">
            ...
            <div class="card bg-light">
                <div class="card-header">Empleados Compañia</div>
                <div class="card-body">
                    <div class="card-title">
                        <button class="btn btn-success w-100" style="border-radius: 15px;" @onclick="@((e)=> MoverAAsignado())">
                            Asociar Empleados Seleccionados
                            <RadzenIcon Icon="arrow_forward" Style="font-size:2rem;" />
                        </button>
                    </div>
                    @* Grid — se migrará en Fase 6 *@
                </div>
            </div>
            ...
            <div class="card bg-light">
                <div class="card-header">Empleados Asignados</div>
                <div class="card-body">
                    <div class="card-title">
                        <button class="btn btn-danger w-100" style="border-radius: 15px;" @onclick="@((e)=> MoverANoAsignado())">
                            <RadzenIcon Icon="arrow_back" Style="font-size:2rem;" />
                            Desasociar Empleados Selección
                        </button>
                    </div>
                    @* Grid — se migrará en Fase 6 *@
                </div>
            </div>
        </div>
    </div>
</div>
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Configuracion/EmpleadoGrupoModal.razor
git commit -m "feat: reemplazar Card/Button BB por HTML Bootstrap en EmpleadoGrupoModal"
```

---

### Tarea 22: EmpleadoJefaturaList.razor — Tabs → RadzenTabs

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Configuracion/EmpleadoJefaturaList.razor`

- [ ] **Reemplazar `<Tabs>/<Tab>` con `<RadzenTabs>/<RadzenTabsItem>`**

```razor
@* ANTES *@
<Tabs @ref="tabs" EnableFadeEffect="true">
    <Tab IsActive="true">
        <TitleTemplate>
            <Icon Name="IconName.LayoutTextWindow" /> @Titulo
        </TitleTemplate>
        <Content>
            @* grid *@
        </Content>
    </Tab>
    <Tab>
        <TitleTemplate>
            <Icon Name="IconName.Upload" /> Importar datos
        </TitleTemplate>
        <Content>
            <ImportarEmpleados ... />
        </Content>
    </Tab>
</Tabs>

@* DESPUÉS *@
<RadzenTabs @ref="tabs" SelectedIndex="0">
    <Tabs>
        <RadzenTabsItem>
            <Template>
                <RadzenIcon Icon="dashboard" /> @Titulo
            </Template>
            @* grid *@
        </RadzenTabsItem>
        <RadzenTabsItem>
            <Template>
                <RadzenIcon Icon="upload" /> Importar datos
            </Template>
            <ImportarEmpleados Destino="@Titulo" OnClickGuardarSelected="GuardarCargaMasiva" />
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>
```

- [ ] **En `@code`, cambiar el tipo de la referencia y el método de cambio de tab**

```csharp
@* ANTES *@
Tabs tabs;
private async Task ShowTabAsync(int index) => await tabs.ShowTabByIndexAsync(index);

@* DESPUÉS *@
RadzenTabs tabs = new();
private void ShowTabAsync(int index) => tabs.SelectedIndex = index;
```

- [ ] **Actualizar la llamada a `ShowTabAsync` en `GuardarCargaMasiva`** (la llamada es `await ShowTabAsync(0)` → cambiar a `ShowTabAsync(0)`)

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Configuracion/EmpleadoJefaturaList.razor
git commit -m "feat: reemplazar Tabs BB por RadzenTabs en EmpleadoJefaturaList"
```

---

## FASE 6 — Grid BB → RadzenDataGrid

**Patrón estándar para popup components** (reciben datos por parámetro y emiten selección):

```razor
@* ANTES *@
<Grid @ref="grid" TItem="T" DataProvider="DataProvider" AllowFiltering="true" ...
      AllowRowClick="true" OnRowClick="OnSelect">
    <GridColumns>
        <GridColumn TItem="T" HeaderText="Col" PropertyName="Prop">@context.Prop</GridColumn>
    </GridColumns>
</Grid>

@code {
    BlazorBootstrap.Grid<T> grid;
    private async Task<GridDataProviderResult<T>> DataProvider(GridDataProviderRequest<T> req) {
        if (datos is null) datos = new List<T>();
        return await Task.FromResult(req.ApplyTo(datos));
    }
    protected async void OnSelect(GridRowEventArgs<T> args) {
        await OnXXXSelected.InvokeAsync(args.Item);
    }
}

@* DESPUÉS *@
<RadzenDataGrid @ref="grid" TItem="T" Data="@datos"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="T" Property="Prop" Title="Col" />
    </Columns>
</RadzenDataGrid>

@code {
    RadzenDataGrid<T> grid = new();
    void OnSelect(DataGridRowMouseEventArgs<T> args) {
        OnXXXSelected.InvokeAsync(args.Data);
    }
}
```

---

### Tarea 23: GeoAgrFincaPop.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/GeoAgrFincaPop.razor`

- [ ] **Reemplazar contenido completo**

```razor
<RadzenDataGrid TItem="GeoAgrFinca" Data="@fincas"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="GeoAgrFinca" Property="Bloque" Title="Bloque" />
        <RadzenDataGridColumn TItem="GeoAgrFinca" Property="Finca" Title="Finca" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<GeoAgrFinca> OnGeoAgrFincaSelected { get; set; }
    [Parameter] public IEnumerable<GeoAgrFinca>? fincas { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<GeoAgrFinca> args)
    {
        OnGeoAgrFincaSelected.InvokeAsync(args.Data);
    }
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/GeoAgrFincaPop.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en GeoAgrFincaPop"
```

---

### Tarea 24: GeoAgrLotePop.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/GeoAgrLotePop.razor`

- [ ] **Reemplazar contenido completo**

```razor
<RadzenDataGrid TItem="GeoAgrLote" Data="@lotes"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="GeoAgrLote" Property="Lote" Title="Lote" />
        <RadzenDataGridColumn TItem="GeoAgrLote" Property="TipoSuelo" Title="Tipo Suelo" />
        <RadzenDataGridColumn TItem="GeoAgrLote" Property="Variedad" Title="Variedad" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<GeoAgrLote> OnGeoAgrLoteSelected { get; set; }
    [Parameter] public IEnumerable<GeoAgrLote>? lotes { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<GeoAgrLote> args)
    {
        OnGeoAgrLoteSelected.InvokeAsync(args.Data);
    }
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/GeoAgrLotePop.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en GeoAgrLotePop"
```

---

### Tarea 25: GeoAgrOtPop.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/GeoAgrOtPop.razor`

- [ ] **Reemplazar contenido completo**

```razor
<RadzenDataGrid TItem="GeoAgrOt" Data="@ots"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="GeoAgrOt" Property="Ot" Title="OT" />
        <RadzenDataGridColumn TItem="GeoAgrOt" Property="NombreOt" Title="Nombre OT" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<GeoAgrOt> OnGeoAgrOtSelected { get; set; }
    [Parameter] public IEnumerable<GeoAgrOt>? ots { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<GeoAgrOt> args)
    {
        OnGeoAgrOtSelected.InvokeAsync(args.Data);
    }
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/GeoAgrOtPop.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en GeoAgrOtPop"
```

---

### Tarea 26: GeoAgrOtLaborPop.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/GeoAgrOtLaborPop.razor`

- [ ] **Reemplazar contenido completo** (nota: el archivo original tiene un carácter `I` erróneo en la línea 11 — se elimina también)

```razor
<RadzenDataGrid TItem="GeoAgrOtLabor" Data="@otLabores"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="GeoAgrOtLabor" Property="Ot" Title="OT" />
        <RadzenDataGridColumn TItem="GeoAgrOtLabor" Property="Labor" Title="Labor" />
        <RadzenDataGridColumn TItem="GeoAgrOtLabor" Property="NombreLabor" Title="Nombre Labor" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<GeoAgrOtLabor> OnGeoAgrOtLaborSelected { get; set; }
    [Parameter] public IEnumerable<GeoAgrOtLabor>? otLabores { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<GeoAgrOtLabor> args)
    {
        OnGeoAgrOtLaborSelected.InvokeAsync(args.Data);
    }
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/GeoAgrOtLaborPop.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en GeoAgrOtLaborPop"
```

---

### Tarea 27: CentroCostoPop.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/CentroCostoPop.razor`

- [ ] **Reemplazar contenido completo**

```razor
<RadzenDataGrid TItem="cCentroCosto" Data="@centrosCosto"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cCentroCosto" Property="IdCCosto" Title="Centro Costo" />
        <RadzenDataGridColumn TItem="cCentroCosto" Property="Descripcion" Title="Descripción" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<cCentroCosto> OnCentroCostoSelected { get; set; }
    [Parameter] public IEnumerable<cCentroCosto>? centrosCosto { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<cCentroCosto> args)
    {
        OnCentroCostoSelected.InvokeAsync(args.Data);
    }
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/CentroCostoPop.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en CentroCostoPop"
```

---

### Tarea 28: ConceptosPop.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/ConceptosPop.razor`

- [ ] **Reemplazar contenido completo**

```razor
<RadzenDataGrid TItem="cConcepto" Data="@conceptos"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cConcepto" Property="Concepto" Title="Concepto" />
        <RadzenDataGridColumn TItem="cConcepto" Property="nominaeq" Title="Equivalencia" />
        <RadzenDataGridColumn TItem="cConcepto" Property="Descripcion" Title="Descripción" />
        <RadzenDataGridColumn TItem="cConcepto" Property="id" Title="Id" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<cConcepto> OnConceptosSelected { get; set; }
    [Parameter] public IEnumerable<cConcepto>? conceptos { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<cConcepto> args)
    {
        OnConceptosSelected.InvokeAsync(args.Data);
    }
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/ConceptosPop.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en ConceptosPop"
```

---

### Tarea 29: ProyectosPop.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/ProyectosPop.razor`

- [ ] **Reemplazar contenido completo**

```razor
<RadzenDataGrid TItem="cPh_Proyecto" Data="@proyectos"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cPh_Proyecto" Property="PROYECTO" Title="Proyecto" />
        <RadzenDataGridColumn TItem="cPh_Proyecto" Property="DESCRIPCION" Title="Descripción" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<cPh_Proyecto> OnProyectoSelected { get; set; }
    [Parameter] public IEnumerable<cPh_Proyecto>? proyectos { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<cPh_Proyecto> args)
    {
        OnProyectoSelected.InvokeAsync(args.Data);
    }
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/ProyectosPop.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en ProyectosPop"
```

---

### Tarea 30: GeoAgrRecursoConceptoPop.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/GeoAgrRecursoConceptoPop.razor`

- [ ] **Reemplazar contenido completo**

```razor
<RadzenDataGrid TItem="GeoAgrRecursoConcepto" Data="@recursos"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="GeoAgrRecursoConcepto" Property="Recurso" Title="Recurso" />
        <RadzenDataGridColumn TItem="GeoAgrRecursoConcepto" Property="Descripcion" Title="Descripción" />
        <RadzenDataGridColumn TItem="GeoAgrRecursoConcepto" Property="TipoFactorMo" Title="Tipo Factor MO" />
        <RadzenDataGridColumn TItem="GeoAgrRecursoConcepto" Property="FactorMo" Title="Factor MO" />
        <RadzenDataGridColumn TItem="GeoAgrRecursoConcepto" Property="ConceptoMo" Title="Concepto MO" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<GeoAgrRecursoConcepto> OnGeoAgrRecursoConceptoSelected { get; set; }
    [Parameter] public IEnumerable<GeoAgrRecursoConcepto>? recursos { get; set; }

    void OnSelect(DataGridRowMouseEventArgs<GeoAgrRecursoConcepto> args)
    {
        OnGeoAgrRecursoConceptoSelected.InvokeAsync(args.Data);
    }
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/GeoAgrRecursoConceptoPop.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en GeoAgrRecursoConceptoPop"
```

---

### Tarea 31: UsuarioPop.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/UsuarioPop.razor`

- [ ] **Reemplazar la sección Grid y el @code**

```razor
@* Mantener el encabezado con título y SpinnerControl *@
@* ... *@

<RadzenDataGrid TItem="EmpleadoExt" Data="@empleados"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnSelect"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="EmpleadoExt" Property="IdNumero" Title="Id Empleado" />
        <RadzenDataGridColumn TItem="EmpleadoExt" Property="Nombre" Title="Nombre" />
        <RadzenDataGridColumn TItem="EmpleadoExt" Property="Email" Title="Correo Electrónico" />
        <RadzenDataGridColumn TItem="EmpleadoExt" Property="DescDepartamento" Title="Departamento" />
        <RadzenDataGridColumn TItem="EmpleadoExt" Property="DescCentroCosto" Title="Centro de Costo" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<string> OnEmpleadoSelected { get; set; }

    public IEnumerable<EmpleadoExt>? empleados { get; set; }
    public string Titulo = "Lista de Empleados";
    public bool InProcess = true;

    protected override async Task OnInitializedAsync()
    {
        empleados = await GetEmpleados();
    }

    private async Task<IEnumerable<EmpleadoExt>> GetEmpleados()
    {
        List<EmpleadoExt> lista = new();
        try
        {
            var emps = await geoConnect.GetEmpleado();
            if (emps != null && emps.Any())
            {
                lista = emps.Select(e => new EmpleadoExt
                {
                    IdNumero         = e.IdNumero,
                    Nombre           = e.Nombre,
                    Email            = e.Email,
                    IdDepartamento   = e.IdDepartamento,
                    IdCCosto         = e.IdCCosto,
                    DescDepartamento = e.Departamento == null ? "" : e.Departamento.DESCRIPCION,
                    DescCentroCosto  = e.CentroCosto  == null ? "" : e.CentroCosto.Descripcion,
                }).ToList();
            }
            else
            {
                await swal.FireAsync("Información", "No se encontraron empleados.", SweetAlertIcon.Info);
            }
        }
        catch (Exception e)
        {
            await swal.FireAsync("Error", $"Error al cargar los empleados. Detalle de error: {e.Message}", SweetAlertIcon.Info);
        }
        InProcess = false;
        StateHasChanged();
        return lista;
    }

    void OnSelect(DataGridRowMouseEventArgs<EmpleadoExt> args)
    {
        OnEmpleadoSelected.InvokeAsync(args.Data.IdNumero);
    }
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/UsuarioPop.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en UsuarioPop"
```

---

### Tarea 32: IconsPop.razor — migrar a Material Icons

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/ComponentesPop/IconsPop.razor`

El componente original enumeraba BB `IconName` enum y emitía `EventCallback<IconName>`.
Después emite `EventCallback<string>` con el nombre del Material Icon.

- [ ] **Reemplazar contenido completo**

```razor
@page "/iconspop"

<RadzenDataGrid TItem="MaterialIconEntry" Data="@listaItems"
                AllowFiltering="true" AllowPaging="true" PageSize="20"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowClick="@OnRowClick"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="MaterialIconEntry" Property="Name" Title="Icono" Width="80px">
            <Template Context="item">
                <RadzenIcon Icon="@item.Name" Style="font-size:1.5rem;" />
            </Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn TItem="MaterialIconEntry" Property="Name" Title="Nombre" />
    </Columns>
</RadzenDataGrid>

@code {
    [Parameter] public EventCallback<string> OnItemSelected { get; set; }

    public class MaterialIconEntry
    {
        public string Name { get; set; } = "";
    }

    private IEnumerable<MaterialIconEntry> listaItems = new List<MaterialIconEntry>
    {
        new() { Name = "add" }, new() { Name = "arrow_back" }, new() { Name = "arrow_forward" },
        new() { Name = "agriculture" }, new() { Name = "cancel" }, new() { Name = "check_circle" },
        new() { Name = "dashboard" }, new() { Name = "delete" }, new() { Name = "event" },
        new() { Name = "face" }, new() { Name = "filter_alt" }, new() { Name = "fingerprint" },
        new() { Name = "format_list_bulleted" }, new() { Name = "group" }, new() { Name = "groups" },
        new() { Name = "history" }, new() { Name = "how_to_reg" }, new() { Name = "location_on" },
        new() { Name = "north_west" }, new() { Name = "notes" }, new() { Name = "person" },
        new() { Name = "save" }, new() { Name = "schedule" }, new() { Name = "search" },
        new() { Name = "upload" }, new() { Name = "zoom_in" },
    };

    void OnRowClick(DataGridRowMouseEventArgs<MaterialIconEntry> args)
    {
        OnItemSelected.InvokeAsync(args.Data.Name);
    }
}
```

- [ ] **Verificar que ningún otro componente llame a `IconsPop` con `EventCallback<IconName>`** — si hay llamadores, actualizar el tipo del parámetro de `IconName` a `string`.

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/ComponentesPop/IconsPop.razor
git commit -m "feat: migrar IconsPop de BB IconName a Material Icons string"
```

---

### Tarea 33: EmpleadoList.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/EmpleadoList.razor`

- [ ] **Reemplazar `<Grid>` y su `@code` relacionado**

```razor
@* Mantener el div card, PageReturn, SpinnerControl *@

<RadzenDataGrid TItem="cEmpleado" Data="@empleado"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cEmpleado" Property="IdNumero" Title="Id Numero" />
        <RadzenDataGridColumn TItem="cEmpleado" Property="Nombre" Title="Nombre de Empleado" />
        <RadzenDataGridColumn TItem="cEmpleado" Property="Identificacion" Title="Identificación" />
    </Columns>
</RadzenDataGrid>
```

- [ ] **Actualizar `@code`** — quitar `BlazorBootstrap.Grid<cEmpleado>`, `modelDataProvider` y su patrón; añadir carga en `OnInitializedAsync`

```csharp
private IEnumerable<cEmpleado> empleado = new List<cEmpleado>();
private string Titulo = "Empleados";
private string UrlRetorno = "./principal";
private bool InProcess = true;

protected override async Task OnInitializedAsync()
{
    await GetEmpleado();
}

private async Task GetEmpleado()
{
    if (loginState.Empleado is null) { NavigationManager.NavigateTo("./"); return; }
    if (string.IsNullOrEmpty(loginState.Empleado.IdNumero)) { NavigationManager.NavigateTo("./"); return; }

    InProcess = true;
    StateHasChanged();
    try
    {
        empleado = (await geoConnect.GetEmpleado()).ToList();
    }
    catch (Exception e)
    {
        await swal.FireAsync("Error", "Al cargar los empleados." + e.Message, SweetAlertIcon.Info);
    }
    InProcess = false;
    StateHasChanged();
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/EmpleadoList.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en EmpleadoList"
```

---

### Tarea 34: EmpleadoExtList.razor

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/EmpleadoExtList.razor`

- [ ] **Reemplazar `<Grid>` con `<RadzenDataGrid>`**

```razor
@if (portal_Config is not null)
{
    <RadzenDataGrid TItem="EmpleadoExt" Data="@empleado"
                    AllowFiltering="true" AllowPaging="true" PageSize="10"
                    AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                    FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                    RowRender="@RowRender"
                    RowClick="@OnSelect"
                    Style="width:100%;">
        <Columns>
            <RadzenDataGridColumn TItem="EmpleadoExt" Property="IdNumero" Title="Número de Empleado" />
            <RadzenDataGridColumn TItem="EmpleadoExt" Property="Nombre" Title="Nombre" />
            <RadzenDataGridColumn TItem="EmpleadoExt" Property="DescDepartamento" Title="Departamento" />
            <RadzenDataGridColumn TItem="EmpleadoExt" Property="DescCentroCosto" Title="Centro de Costo" />
            <RadzenDataGridColumn TItem="EmpleadoExt" Property="DescGrupo" Title="Grupo" />
            <RadzenDataGridColumn TItem="EmpleadoExt" Property="DescPlanilla" Title="Nómina" />
            @if (portal_Config!.USARECONOCIMIENTOFACIAL)
            {
                <RadzenDataGridColumn TItem="EmpleadoExt" Property="TieneRostro" Title="Enrolado" Width="100px">
                    <Template Context="emp">
                        @if (emp.TieneRostro)
                        {
                            <RadzenIcon Icon="face" Style="font-size:1.5rem;color:var(--rz-success);" />
                            <RadzenIcon Icon="delete" Style="font-size:1.5rem;color:var(--rz-danger);cursor:pointer;" @onclick:stopPropagation="true" @onclick="() => EliminarRostros(emp)" />
                        }
                        else
                        {
                            <RadzenIcon Icon="cancel" Style="font-size:1.5rem;color:var(--rz-danger);" />
                        }
                    </Template>
                </RadzenDataGridColumn>
            }
        </Columns>
    </RadzenDataGrid>
}
```

- [ ] **Actualizar `@code`** — quitar `BlazorBootstrap.Grid<EmpleadoExt>` y `modelDataProvider`, añadir `OnInitializedAsync`, adaptar `OnSelect` y `RowRender`

```csharp
private IEnumerable<EmpleadoExt> empleado = new List<EmpleadoExt>();
// ... resto de campos sin cambios ...

protected override async Task OnInitializedAsync()
{
    await GetEmpleado();
}

// Quitar modelDataProvider por completo.
// GetEmpleado() queda igual, pero sin el patrón DataProvider.

protected void OnSelect(DataGridRowMouseEventArgs<EmpleadoExt> args)
{
    // lógica igual, pero args.Data en lugar de args.Item
    if (portal_Config!.USARECONOCIMIENTOFACIAL)
    {
        if (portal_Config.CANTMAXPLANTILLAS > args.Data.CantPlantillasRegistradas)
            NavigationManager.NavigateTo($"./enrolarempleado/{args.Data.IdNumero}");
        else
            swal.FireAsync("Atención", "Ya se han registrado la cantidad de plantillas de rostros permitida para el colaborador.", SweetAlertIcon.Warning);
    }
    else
        swal.FireAsync("Atención", "No ha habilitado la opcion de Reconocimiento facial para esta compañía", SweetAlertIcon.Warning);
}

void RowRender(RowRenderEventArgs<EmpleadoExt> args)
{
    if (args.Data.Estado == 'F')
        args.Attributes.Add("class", "table-danger");
}

// EliminarRostros: al final reemplazar grid.RefreshDataAsync() por:
empleado = new List<EmpleadoExt>();
await GetEmpleado();
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/EmpleadoExtList.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en EmpleadoExtList"
```

---

### Tarea 35: MarcasLocalizacion.razor (Grid)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Procesos/MarcasLocalizacion.razor`

- [ ] **Reemplazar `<Grid>` dentro del AccordionItem "Detalle" (ya migrado a RadzenPanel)**

```razor
<RadzenDataGrid TItem="MarcaDiariaExt" Data="@marcas"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowRender="@RowRender"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="MarcaDiariaExt" Property="IdNumero" Title="Id Colaborador" />
        <RadzenDataGridColumn TItem="MarcaDiariaExt" Property="Nombre" Title="Nombre" />
        <RadzenDataGridColumn TItem="MarcaDiariaExt" Property="FechaMarca" Title="Fecha">
            <Template Context="m">@m.FechaMarca.ToString("dd/MM/yyyy")</Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn TItem="MarcaDiariaExt" Property="HoraMarca" Title="Hora" />
        <RadzenDataGridColumn TItem="MarcaDiariaExt" Property="TipoMarca" Title="Tipo" />
        <RadzenDataGridColumn TItem="MarcaDiariaExt" Property="Latitud" Title="Latitud" />
        <RadzenDataGridColumn TItem="MarcaDiariaExt" Property="Longitud" Title="Longitud" />
    </Columns>
</RadzenDataGrid>
```

- [ ] **Actualizar `@code`** — quitar `BlazorBootstrap.Grid<MarcaDiariaExt>` y `modelDataProvider`; la carga pasa a `OnInitializedAsync`; `BuscarMarcas` recarga directamente

```csharp
private IEnumerable<MarcaDiariaExt>? marcas;
// Quitar: BlazorBootstrap.Grid<MarcaDiariaExt>? grid;
// Quitar: modelDataProvider()

protected override async Task OnInitializedAsync()
{
    await Inicializar();
    await GetMarcas();
}

// BuscarMarcas — quitar grid.RefreshDataAsync(), reemplazar por:
public async Task BuscarMarcas()
{
    marcas = null;
    await GetMarcas();
    StateHasChanged();
}

// Añadir RowRender para el color por tipo:
void RowRender(RowRenderEventArgs<MarcaDiariaExt> args)
{
    string color = args.Data.tipo switch
    {
        1 => "table-success",
        2 => "table-danger",
        3 => "table-warning",
        _ => ""
    };
    if (!string.IsNullOrEmpty(color))
        args.Attributes.Add("class", color);
}
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Procesos/MarcasLocalizacion.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en MarcasLocalizacion"
```

---

### Tarea 36: EmpleadoJefaturaList.razor (Grid)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Configuracion/EmpleadoJefaturaList.razor`

- [ ] **Reemplazar `<Grid>` dentro del primer RadzenTabsItem**

```razor
<RadzenDataGrid TItem="EmpleadoJefaturaExt" Data="@listaEmpleadoJefatura"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                RowRender="@RowRender"
                RowClick="@OnEdit"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="EmpleadoJefaturaExt" Property="IdNumero" Title="Id de Empleado" />
        <RadzenDataGridColumn TItem="EmpleadoJefaturaExt" Property="NombreEmpleado" Title="Nombre de Empleado" />
        <RadzenDataGridColumn TItem="EmpleadoJefaturaExt" Property="Departamento" Title="Departamento" />
        <RadzenDataGridColumn TItem="EmpleadoJefaturaExt" Property="Grupo" Title="Grupo" />
        <RadzenDataGridColumn TItem="EmpleadoJefaturaExt" Property="NombreResponsable" Title="Responsable (Titular)" />
        <RadzenDataGridColumn TItem="EmpleadoJefaturaExt" Property="NombreSuplente" Title="Responsable (Suplente)" />
    </Columns>
</RadzenDataGrid>
```

- [ ] **Actualizar `@code`** — quitar `BlazorBootstrap.Grid<EmpleadoJefaturaExt>` y `DireccionLDataProvider`; mover carga a `OnInitializedAsync`; adaptar `OnEdit`, `RowRender`, refresh de grid

```csharp
private IEnumerable<EmpleadoJefaturaExt> listaEmpleadoJefatura = new List<EmpleadoJefaturaExt>();
// Quitar: BlazorBootstrap.Grid<EmpleadoJefaturaExt> grid;
// Quitar: DireccionLDataProvider()

protected override async Task OnInitializedAsync()
{
    try
    {
        if (Titulo is null) Titulo = "Jefaturas Directas";
        listaEmpleadoJefatura = await GetEmpleadoJefatura();
    }
    catch (Exception e)
    {
        await swal.FireAsync("Error", "Se presentó un error al procesar solicitud: " + e.Message, SweetAlertIcon.Error);
    }
    StateHasChanged();
}

void OnEdit(DataGridRowMouseEventArgs<EmpleadoJefaturaExt> args)
{
    RedirectTo($"./empleadojefaturadetalle/{args.Data.IdNumero}/2");
}

void RowRender(RowRenderEventArgs<EmpleadoJefaturaExt> args)
{
    if (string.IsNullOrEmpty(args.Data.IdNumeroResponsable) && string.IsNullOrEmpty(args.Data.IdNumeroSuplente))
        args.Attributes.Add("class", "table-danger");
}

// En GuardarCargaMasiva, reemplazar:
//   await grid.RefreshDataAsync();  →  listaEmpleadoJefatura = await GetEmpleadoJefatura(); StateHasChanged();
// Ajustar también:
//   listaEmpleadoJefatura = null;  →  listaEmpleadoJefatura = new List<EmpleadoJefaturaExt>();
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Configuracion/EmpleadoJefaturaList.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid en EmpleadoJefaturaList"
```

---

### Tarea 37: EmpleadoGrupoModal.razor (Grid con selección múltiple)

**Archivos:**
- Modificar: `LaboresNet/Components/Pages/Configuracion/EmpleadoGrupoModal.razor`

Este componente necesita dos grids con selección múltiple. Se usa `@bind-SelectedItems` de RadzenDataGrid.

- [ ] **Reemplazar los 2 `<Grid>` con `<RadzenDataGrid>` con selección múltiple**

```razor
@* Grid Compañía *@
<RadzenDataGrid @ref="gridCompania" TItem="cEmpleado"
                Data="@empleadoCompania"
                AllowFiltering="true" AllowPaging="false"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                SelectionMode="DataGridSelectionMode.Multiple"
                @bind-Value="selectedEmpleadoC"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cEmpleado" Property="IdNumero" Title="Empleado" />
        <RadzenDataGridColumn TItem="cEmpleado" Property="Nombre" Title="Nombre" />
    </Columns>
</RadzenDataGrid>

@* Grid Asignado *@
<RadzenDataGrid @ref="gridAsignado" TItem="cEmpleado"
                Data="@empleadoAsignado"
                AllowFiltering="true" AllowPaging="false"
                AllowSorting="true" FilterMode="FilterMode.SimpleWithMenu"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                SelectionMode="DataGridSelectionMode.Multiple"
                @bind-Value="selectedEmpleadoA"
                Style="width:100%;">
    <Columns>
        <RadzenDataGridColumn TItem="cEmpleado" Property="IdNumero" Title="Empleado" />
        <RadzenDataGridColumn TItem="cEmpleado" Property="Nombre" Title="Nombre" />
    </Columns>
</RadzenDataGrid>
```

- [ ] **Actualizar `@code`** — cambiar tipos de referencia y listas de selección; cambiar `RefreshDataAsync()` por `Reload()`

```csharp
// ANTES:
BlazorBootstrap.Grid<cEmpleado> gridCompania = default!;
BlazorBootstrap.Grid<cEmpleado> gridAsignado = default!;
private List<cEmpleado> selectedEmpleadoC = new();
private List<cEmpleado> selectedEmpleadoA = new();

// DESPUÉS:
RadzenDataGrid<cEmpleado> gridCompania = new();
RadzenDataGrid<cEmpleado> gridAsignado = new();
private IList<cEmpleado> selectedEmpleadoC = new List<cEmpleado>();
private IList<cEmpleado> selectedEmpleadoA = new List<cEmpleado>();

// Quitar OnSelectedItemsChanged y OnSelectedItemsChanged2
// (la selección ya la maneja @bind-Value)

// Reemplazar en ActualizarGridsAsync:
// ANTES: await Task.WhenAll(gridCompania.RefreshDataAsync(), gridAsignado.RefreshDataAsync());
// DESPUÉS:
private async Task ActualizarGridsAsync()
{
    await Task.WhenAll(gridCompania.Reload(), gridAsignado.Reload());
    await OnSelect();
}

// En MoverAAsignado / MoverANoAsignado:
// Cambiar selectedEmpleadoC.Any() → selectedEmpleadoC.Count > 0
// Cambiar foreach (var empleado in selectedEmpleadoC) → foreach (var empleado in selectedEmpleadoC.ToList())
// Cambiar foreach (var empleado in selectedEmpleadoA) → foreach (var empleado in selectedEmpleadoA.ToList())
```

- [ ] **Commit**
```
git add LaboresNet/Components/Pages/Configuracion/EmpleadoGrupoModal.razor
git commit -m "feat: migrar Grid BB → RadzenDataGrid con selección múltiple en EmpleadoGrupoModal"
```

---

## FASE 7 — Limpieza de imports

### Tarea 38: _Imports.razor — eliminar `@using BlazorBootstrap`

**Archivos:**
- Modificar: `LaboresNet/Components/_Imports.razor`

- [ ] **Eliminar la línea `@using BlazorBootstrap;`** del archivo `_Imports.razor`

El archivo queda así (solo se elimina esa línea; SweetAlert2 permanece):

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using System.Text.Json.Nodes;
@using Microsoft.Extensions.Configuration

@using LaboresNet
@using LaboresNet.Components
@using LaboresNet.Components.Layout
@using LaboresNet.Components.Pages
@using LaboresNet.Components.Pages.ComponentesPop
@using LaboresNet.Components.Pages.Configuracion
@using LaboresNet.Components.Pages.Procesos
@using LaboresNet.Components.Pages.RelojMarcador
@using LaboresNet.Data
@using LaboresNet.Data.Interfaces

@using CurrieTechnologies.Razor.SweetAlert2
@using PortalEmpleadoGeotime.Client.Data
@using Radzen
@using Radzen.Blazor

@using PortalEmpleadoGeotime.Client
@using PortalEmpleadoGeotime.Client.Modelo
@using PortalEmpleadoGeotime.Client.Data.Interfaces
@using PortalEmpleadoGeotime.Client.Data.State
@using PortalEmpleadoGeotime.Client.Modelo.Utils

@using com.gsitcr.geotime.Models
@using com.gsitcr.geotime.Models.Request
@using com.gsitcr.geotime.Models.Response
@using com.gsitcr.geotime.Models.Utils
@using TicoFrutLibs.Models

@inject IJSRuntime JsRuntime
@inject NavigationManager NavigationManager
@inject SweetAlertService swal
@inject IGeoTimeConnect geoConnect
@inject LoginState loginState
@inject IGridExtensions gridExtensions
@inject Radzen.DialogService dialogService
@inject ExportData exportData
@inject ITicoFrutConnect ticoFrutConnect
```

- [ ] **Verificar que el proyecto compile sin errores** (el paquete NuGet de BlazorBootstrap aún puede estar en el .csproj si se quiere eliminar después; la compilación debe funcionar sin el `@using`)

```bash
dotnet build LaboresNet/LaboresNet.csproj
```
Resultado esperado: `Build succeeded` sin errores.

- [ ] **Commit final**
```
git add LaboresNet/Components/_Imports.razor
git commit -m "feat: eliminar @using BlazorBootstrap de _Imports.razor — migración completa"
```

---

## Verificación post-migración

Después de todas las tareas, verificar manualmente:

- [ ] Spinner aparece correctamente en todas las páginas que lo usan
- [ ] Iconos se renderizan (Material Icons cargadas vía Radzen)
- [ ] Paneles colapsables funcionan (Accordion → RadzenPanel)
- [ ] DatePicker de radzen acepta valores `DateOnly`
- [ ] NumberInput acepta decimales
- [ ] Switch (EmpleadoDetalle) se vincula a `Habilitado` correctamente
- [ ] Tabs en EmpleadoJefaturaList cambia al tab 0 después de guardar
- [ ] Grids de popup (Finca, Lote, OT, etc.) abren, muestran datos y emiten selección al hacer clic
- [ ] EmpleadoGrupoModal selecciona múltiples empleados y los mueve correctamente
- [ ] EmpleadoExtList muestra colores de fila para empleados inactivos
- [ ] EmpleadoJefaturaList muestra colores de fila para jefaturas sin asignar
