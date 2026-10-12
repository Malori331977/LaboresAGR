# Sistema de Labores — Migración a marca Geotime / SoftlandCA

Fecha: 2026-10-09

Nueva versión de Labores, integrada exclusivamente a Geotime y SoftlandCA. Este documento resume lo decidido y lo implementado.

## 1. Nombres

| Antes | Ahora |
|---|---|
| Avances de Mano de Obra / Registro de Avances de Mano de Obra | **Sistema de Labores** |
| Carpeta/proyecto `AvancesMO`, `AvancesMO.csproj`, `AvancesMO.slnx` | `LaboresNet/`, `LaboresNet.csproj`, `LaboresNet.slnx` |
| Namespace `AvancesMO.*` | `LaboresNet.*` |
| Carpeta raíz `LaboresNetMaui` | **Pendiente** renombrar a `LaboresNet` (hacerlo fuera de VS/sesión) |

### Conservado a propósito (no cambiar sin plan de migración)
- `ApplicationId`: `com.gsitcr.geotime.avancesmo` (cambiarlo instala otra app y rompe actualizaciones).
- Keystore `Platforms/Android/AvancesMO.keystore` y alias `AvancesMO` (firma de actualizaciones).

## 2. Paleta (libro de marca Geotime)

Fuente: `D:\Source\Geotime\GeotimeNetServerProject\GeotimeNetServerProject\wwwroot\lib\others\css\custom-radzen.css` (tema *rz-default* "Degradado Celeste"). Tipografía: **Poppins**.

| Token | Color | Uso |
|---|---|---|
| gt-900 | `#052767` | Footer, títulos |
| gt-700 | `#1a76cb` | Texto de acento, primario sobre claro |
| gt-600 | `#265AE8` | Fin de degradados |
| gt-500 | `#2192FF` | Primario, botones, ícono/splash |
| gt-400 | `#1AA5EA` | Inicio de degradado de header |
| gt-aqua | `#ace6f3` | Destacados sobre fondo oscuro |
| gt-200 / 100 / 50 | `#deeef6` / `#EBF5FC` / `#f0f7ff` | Bordes, chips, fondos |
| Oscuro | `#0f1923`, `#162032`, `#1e3a5f` | Paleta dark (no aplicada aún) |

Patrones: header `linear-gradient(90deg,#1AA5EA,#2192FF 60%,#265AE8)`; footer `linear-gradient(90deg,#052767,#1a76cb)`; cabecera de panel `#3d9de0 → #55b0e8`; login `linear-gradient(165deg,#052767,#1a76cb 55%,#1AA5EA)`.

## 3. Mapeo de colores aplicado

| Antes (TicoFrut) | Ahora |
|---|---|
| `#017F01` | `#1a76cb` |
| `#015901` | `#052767` |
| `#38E54D` | `#1AA5EA` |
| `#e8f5e9` | `#EBF5FC` |
| `#c8e6c9` | `#deeef6` |
| `#a5d6a7` | `#ace6f3` |
| `#f5f5f5` | `#f0f7ff` |
| `#e0e0e0` | `#deeef6` |
| `#1b6ec2`, `#007bff` | `#2192FF` |
| `#1565c0` / `#e3f2fd` | `#1a76cb` / `#EBF5FC` |

No se tocaron colores semánticos (verde/ámbar/rojo de estados, botones éxito/eliminar, íconos `bi-green`/`bi-red`).

## 4. Archivos modificados

- `LaboresNet/wwwroot/css/app.css` — tokens `--gt-*`, variables Radzen (`--rz-primary`…), Poppins, login, panel-brand, semáforo.
- `LaboresNet/Components/Layout/MainLayout.razor` — header/footer con degradado, logo SoftlandCA blanco.
- `LaboresNet/Components/Pages/Home.razor` — login con logo blanco, semáforo en modo oscuro.
- `LaboresNet/Components/Pages/Procesos/*.razor` — colores inline.
- `LaboresNet/wwwroot/index.html` — título, Poppins, `theme-color`.
- `LaboresNet/MainPage.xaml`, `LaboresNet.csproj` (MauiIcon/Splash), `Platforms/Android/Resources/values/colors.xml`, `Resources/AppIcon/*.svg`.
- Nuevo: `LaboresNet/wwwroot/img/LogoSoftlandCA_blanco.png`.

## 5. Mockups

- `Documentacion/mockup-marca-geotime.html` (autocontenido, logo Geotime) y `mockup-artifact.html` (versión publicada, logo SoftlandCA blanco, claro/oscuro).
- Publicado: https://claude.ai/artifact/AXA54VcDmn63KS4E3hpPXw (privado).

## 6. Git

- Repositorio anterior (`origin` = `github.com/Malori331977/LaboresNet`, ramas `master`/`develop`/`CambioVersion1`) respaldado en `D:\Source\Geotime\LaboresNetMaui.git-backup`.
- Repositorio nuevo: `git init -b main`, sin commits ni remoto.

## 7. Pendientes

1. Renombrar carpeta raíz `LaboresNetMaui` → `LaboresNet`.
2. Crear el repo nuevo en GitHub, hacer primer commit y `git remote add origin`.
3. Revisar `.gitignore`: excluir `*.keystore`, `.vs/`, `bin/`, `obj/`.
4. Verificar visualmente en ejecución: header, login, paneles, botón "agregar" (aún `btn-success` verde).
5. Diseñar ícono de app nuevo con la marca (el actual solo cambió de color).
6. Valorar tema oscuro (paleta `gt-d-*`) y cambio de `ApplicationId` con plan de migración.
