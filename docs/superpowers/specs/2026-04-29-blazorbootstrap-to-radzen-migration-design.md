# Spec: Migración Blazor.Bootstrap → Radzen

**Fecha:** 2026-04-29  
**Proyecto:** LaboresNet (.NET MAUI Blazor)  
**Alcance:** Eliminar todos los controles de Blazor.Bootstrap y sustituirlos por equivalentes Radzen o HTML/Bootstrap nativo. SweetAlert2 queda sin cambios.

---

## 1. Contexto

El proyecto ya usa Radzen extensivamente (RadzenDataGrid, RadzenDropDown, RadzenDatePicker, etc.). Blazor.Bootstrap coexiste actualmente como segunda librería de UI, generando dependencia dual. El objetivo es dejar una sola librería de componentes.

**SweetAlert2 (`CurrieTechnologies.Razor.SweetAlert2`) NO se toca.** Permanece como está.

---

## 2. Inventario de componentes a migrar

| # | Componente BB | Reemplazo | Archivos | Complejidad |
|---|---|---|---|---|
| 1 | `<Spinner>` en SpinnerControl.razor | `<div class="spinner-border text-warning">` Bootstrap nativo | 1 | Baja |
| 2 | `<Preload>` en Home.razor | Eliminar (SpinnerControl ya cubre el caso) | 1 | Baja |
| 3 | `<Icon Name="IconName.X">` | `<RadzenIcon Icon="material_name"/>` | 19 | Media |
| 4 | `<Accordion>/<AccordionItem>` | `<RadzenPanel AllowCollapse="true">` | 8 | Media |
| 5 | `<DateInput TValue="DateOnly">` | `<RadzenDatePicker TValue="DateOnly"/>` | 3 | Baja |
| 6 | `<NumberInput TValue="decimal?">` | `<RadzenNumeric TValue="decimal?"/>` | 3 | Baja |
| 7 | `<Switch @bind-Value>` | `<RadzenCheckBox @bind-Value>` | 1 | Baja |
| 8 | `<Button Color="ButtonColor.X">` | `<RadzenButton ButtonStyle="ButtonStyle.X">` | 1 | Baja |
| 9 | `<Card>/<CardBody>/<CardHeader>/<CardTitle>` | `<div class="card/card-body/card-header">` Bootstrap | 1 | Baja |
| 10 | `<Tabs>/<Tab>` | `<RadzenTabs>/<RadzenTabsItem>` | 1 | Media |
| 11 | `<Grid>/<GridColumn>` + DataProvider callback | `<RadzenDataGrid>/<RadzenDataGridColumn>` + `Data=@lista` | 13 | Alta |
| 12 | `BlazorBootstrap.Grid<T>` en `@code` | `RadzenDataGrid<T>` | 13 | Alta |
| 13 | `GridDataProviderResult/Request<T>` | Binding directo a lista (carga en OnInitializedAsync) | 13 | Alta |
| 14 | `GridRowEventArgs<T>` en OnRowClick | `DataGridRowMouseEventArgs<T>` en RowClick | 3 | Media |
| 15 | `GridSelectionMode.Multiple` + `SelectedItemsChanged` | Columna con `RadzenCheckBox` y lista propia | 1 | Media |
| 16 | `grid.RefreshDataAsync()` | `grid.Reload()` | 2 | Baja |
| 17 | `@using BlazorBootstrap` en archivos individuales | Eliminar | 17 | Baja |
| 18 | `IconsPop.razor` (itera `Enum.GetValues<IconName>()`) | Convertir para listar Material Icons como strings | 1 | Media |

---

## 3. Mapeo de iconos (IconName → Material Icons)

| `IconName.X` | Material Icon string |
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

### Atributos de tamaño y color

| BB | Radzen/HTML |
|---|---|
| `Size="IconSize.x2"` | `Style="font-size:1.5rem;"` |
| `Size="IconSize.x5"` | `Style="font-size:2rem;"` |
| `Color="IconColor.Success"` | `Style="color:var(--rz-success);"` |
| `Color="IconColor.Danger"` | `Style="color:var(--rz-danger);"` |
| `Class="me-1"` | `Style="margin-right:0.25rem;"` o mantener `class="me-1"` en wrapper |

Cuando `<Icon>` tiene `@onclick`, el handler se mueve al elemento `<span>` wrapper o directamente a `<RadzenIcon>`.

---

## 4. Migración de `<Grid>` (fase más compleja)

### 4.1 Patrón DataProvider → Data binding directo

**Antes (BB):**
```razor
<Grid @ref="grid" TItem="T" DataProvider="MiDataProvider" ...>
    <GridColumns>
        <GridColumn TItem="T" HeaderText="Col" PropertyName="Prop">
            @context.Prop
        </GridColumn>
    </GridColumns>
</Grid>

@code {
    BlazorBootstrap.Grid<T> grid;
    private async Task<GridDataProviderResult<T>> MiDataProvider(GridDataProviderRequest<T> request) {
        if (lista is null) lista = await CargarDatos();
        return await Task.FromResult(request.ApplyTo(lista));
    }
}
```

**Después (Radzen):**
```razor
<RadzenDataGrid @ref="grid" TItem="T" Data="@lista"
                AllowFiltering="true" AllowPaging="true" PageSize="10"
                AllowSorting="true" FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                FilterMode="FilterMode.SimpleWithMenu" ...>
    <Columns>
        <RadzenDataGridColumn TItem="T" Property="Prop" Title="Col" />
    </Columns>
</RadzenDataGrid>

@code {
    RadzenDataGrid<T> grid;
    private IEnumerable<T> lista;

    protected override async Task OnInitializedAsync() {
        lista = await CargarDatos();
    }
}
```

### 4.2 Row click

```csharp
// BB: OnRowClick="OnEdit" con GridRowEventArgs<T>
protected void OnEdit(GridRowEventArgs<T> args) { ... args.Item ... }

// Radzen: RowClick="OnEdit" con DataGridRowMouseEventArgs<T>
void OnEdit(DataGridRowMouseEventArgs<T> args) { ... args.Data ... }
```

### 4.3 Row CSS class

```csharp
// BB: RowClass="GetRowClass" donde GetRowClass(T item) => string
// Radzen: RowRender="RowRender" donde:
void RowRender(RowRenderEventArgs<T> args) {
    args.Attributes.Add("class", GetRowClass(args.Data));
}
```

### 4.4 Selección múltiple (EmpleadoGrupoModal)

BB usaba `AllowSelection="true" SelectionMode="GridSelectionMode.Multiple" SelectedItemsChanged="Handler"`.  
Radzen: columna extra con `RadzenCheckBox` y lista `selectedItems` propia, igual al patrón ya implementado en GeoAgrLaborAuxiliarDetalle.

### 4.5 Refresh

```csharp
// BB:  await grid.RefreshDataAsync();
// Radzen: await grid.Reload();
```

---

## 5. Migración de `<Accordion>/<AccordionItem>`

**Antes:**
```razor
<Accordion AlwaysOpen="true">
    <AccordionItem Active="true">
        <TitleTemplate>Título</TitleTemplate>
        <Content>Contenido</Content>
    </AccordionItem>
</Accordion>
```

**Después:**
```razor
<RadzenPanel AllowCollapse="true" Style="margin-bottom:0.5rem;">
    <HeaderTemplate>Título</HeaderTemplate>
    <ChildContent>Contenido</ChildContent>
</RadzenPanel>
```

`AccordionItem Active="true"` → el panel arranca expandido por defecto (comportamiento default de RadzenPanel).  
`AlwaysOpen="true"` → cada RadzenPanel es independiente, todos pueden estar abiertos a la vez: comportamiento equivalente.

---

## 6. Migración de `<Tabs>/<Tab>` (EmpleadoJefaturaList)

**Antes:**
```razor
<Tabs @ref="tabs" EnableFadeEffect="true">
    <Tab IsActive="true">
        <TitleTemplate>...</TitleTemplate>
        <Content>...</Content>
    </Tab>
</Tabs>
@code { Tabs tabs; await tabs.ShowTabByIndexAsync(0); }
```

**Después:**
```razor
<RadzenTabs @ref="tabs" SelectedIndex="0">
    <Tabs>
        <RadzenTabsItem Text="Título">...</RadzenTabsItem>
    </Tabs>
</RadzenTabs>
@code { RadzenTabs tabs; tabs.SelectedIndex = 0; }
```

---

## 7. Migración de inputs especializados

| BB | Radzen | Notas |
|---|---|---|
| `<DateInput TValue="DateOnly" @bind-Value="v" EnableMinMax="true" Min="min" Max="max">` | `<RadzenDatePicker TValue="DateOnly" @bind-Value="v" Min="min.ToDateTime(TimeOnly.MinValue)" Max="max.ToDateTime(TimeOnly.MaxValue)" DateFormat="dd/MM/yyyy"/>` | Min/Max cambia a DateTime |
| `<NumberInput TValue="decimal?" @bind-Value="v" placeholder="0.00">` | `<RadzenNumeric TValue="decimal?" @bind-Value="v" Placeholder="0.00" Format="n2"/>` | |

---

## 8. SpinnerControl y Preload

**SpinnerControl.razor** (antes usa `<Spinner Type="SpinnerType.Dots" Size="SpinnerSize.ExtraLarge" Color="SpinnerColor.Warning">`):
```razor
<div class="text-center">
    <div class="spinner-border text-warning" role="status" style="width:3rem;height:3rem;">
        <span class="visually-hidden">Cargando...</span>
    </div>
    <br />Cargando...
</div>
```

**Home.razor**: eliminar `<Preload LoadingText="Iniciando sesión en portal..."/>` sin reemplazo.

---

## 9. Limpieza final

- Eliminar `@using BlazorBootstrap` de `_Imports.razor`.
- Eliminar `@using BlazorBootstrap` de los 16 archivos individuales que lo declaran localmente.
- Eliminar referencias a tipos BB en `@code`: `BlazorBootstrap.Grid<T>`, `GridDataProviderResult<T>`, `GridDataProviderRequest<T>`, `GridRowEventArgs<T>`, `GridSelectionMode`, `IconName`, `IconSize`, `IconColor`, `ButtonColor`, `CardColor`, `Tabs`.
- Verificar que el proyecto compile sin errores antes de eliminar el paquete NuGet.

---

## 10. Archivos afectados por fase

### Fase 1 — SpinnerControl + Preload
- `Components/Layout/SpinnerControl.razor`
- `Components/Pages/Home.razor`

### Fase 2 — Iconos (19 archivos)
- `Layout/PageReturn.razor`, `Layout/PageMant.razor`
- `Pages/Home.razor`, `Pages/Principal.razor`
- `Pages/ComponentesPop/IconsPop.razor`
- `Pages/Configuracion/EmpleadoJefaturaList.razor`, `EmpleadoJefaturaDetalle.razor`, `EmpleadoGrupoModal.razor`
- `Pages/Procesos/EmpleadoDetalle.razor`, `EmpleadoExtList.razor`, `EmpleadoMarcaList.razor`
- `Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`, `GeoAgrLaborAuxiliarList.razor`
- `Pages/Procesos/Marcas.razor`, `MarcasLocalizacion.razor`, `MarcasDistribucionDetalle.razor`, `MarcasDistribucionList.razor`
- `Pages/Procesos/EnrolarEmpleado.razor`
- `Pages/RelojMarcador/RegistroMarcas.razor`

### Fase 3 — Accordion (8 archivos)
- `Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`, `GeoAgrLaborAuxiliarList.razor`
- `Pages/Procesos/Marcas.razor`, `EmpleadoMarcaList.razor`, `MarcasDistribucionDetalle.razor`, `MarcasDistribucionList.razor`, `MarcasLocalizacion.razor`
- `Pages/RelojMarcador/RegistroMarcas.razor`

### Fase 4 — DateInput / NumberInput (3 archivos)
- `Pages/Procesos/GeoAgrLaborAuxiliarDetalle.razor`
- `Pages/Procesos/GeoAgrLaborAuxiliarList.razor`
- `Pages/Procesos/MarcasDistribucionDetalle.razor`

### Fase 5 — Switch, Button, Card, Tabs (2 archivos)
- `Pages/Procesos/EmpleadoDetalle.razor` (Switch)
- `Pages/Configuracion/EmpleadoGrupoModal.razor` (Button, Card)
- `Pages/Configuracion/EmpleadoJefaturaList.razor` (Tabs)

### Fase 6 — Grid (13 archivos)
- `Pages/ComponentesPop/GeoAgrFincaPop.razor`, `GeoAgrLotePop.razor`, `GeoAgrOtPop.razor`, `GeoAgrOtLaborPop.razor`
- `Pages/ComponentesPop/CentroCostoPop.razor`, `ConceptosPop.razor`, `ProyectosPop.razor`
- `Pages/ComponentesPop/GeoAgrRecursoConceptoPop.razor`, `UsuarioPop.razor`, `IconsPop.razor`
- `Pages/Procesos/EmpleadoList.razor`, `EmpleadoExtList.razor`, `MarcasLocalizacion.razor`
- `Pages/Configuracion/EmpleadoJefaturaList.razor`, `EmpleadoGrupoModal.razor`

### Fase 7 — Limpieza de @using BlazorBootstrap
- `Components/_Imports.razor` + 16 archivos individuales

---

## 11. Criterio de éxito

- El proyecto compila sin errores ni warnings relacionados con `BlazorBootstrap`.
- Todas las páginas renderizan correctamente.
- Los grids de selección múltiple (EmpleadoGrupoModal) funcionan.
- El cambio de tabs en EmpleadoJefaturaList funciona (incluyendo `ShowTabByIndexAsync`).
- Los filtros, paginación y ordenamiento de los grids migrados funcionan.
