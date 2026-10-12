<#
.SYNOPSIS
    Genera el keystore de firma de la app LaboresNet (com.gsitcr.geotime.avancesmo).

.DESCRIPTION
    Crea un keystore PKCS12 con una clave RSA 2048 valida 30 anios, usando keytool
    del JDK que instala Visual Studio / Android Studio.

    IMPORTANTE: este keystore es la identidad de la app. Si se pierde, Android ya no
    acepta actualizaciones sobre las instalaciones existentes (hay que desinstalar y
    reinstalar en cada dispositivo). Respaldelo fuera del repositorio.

    El script NUNCA sobrescribe un keystore existente.

.PARAMETER KeystorePath
    Ruta del archivo .keystore a generar. Por defecto LaboresNet\avancesmo-upload.keystore,
    que es la ruta que espera AndroidSigningKeyStore en el csproj.

.PARAMETER Alias
    Alias de la clave dentro del keystore. Debe coincidir con AndroidSigningKeyAlias.

.PARAMETER Password
    Contrasena del keystore y de la clave. Si se omite, el script la pide de forma
    oculta por consola. Debe tener al menos 6 caracteres (limite de keytool).

.PARAMETER ValidityDays
    Dias de validez del certificado. Google Play exige que venza despues de 2033.

.EXAMPLE
    .\scripts\New-SigningKey.ps1
    Pide la contrasena por consola y genera LaboresNet\avancesmo-upload.keystore.

.EXAMPLE
    .\scripts\New-SigningKey.ps1 -KeystorePath D:\Llaves\avancesmo-upload.keystore
    Genera el keystore fuera del repositorio (recomendado para produccion).
#>
[CmdletBinding()]
param(
    [string]   $KeystorePath = (Join-Path $PSScriptRoot '..\LaboresNet\avancesmo-upload.keystore'),
    [string]   $Alias        = 'avancesmo-upload',
    [SecureString] $Password,
    [int]      $ValidityDays = 10950,
    [string]   $CommonName   = 'LaboresNet',
    [string]   $OrgUnit      = 'TI',
    [string]   $Org          = 'GSITCR',
    [string]   $Locality     = 'San Jose',
    [string]   $State        = 'San Jose',
    [string]   $Country      = 'CR'
)

$ErrorActionPreference = 'Stop'

function Resolve-Keytool {
    $cmd = Get-Command keytool.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $roots = @(
        "$env:JAVA_HOME\bin\keytool.exe",
        "$env:ProgramFiles\Microsoft\jdk-*\bin\keytool.exe",
        "$env:ProgramFiles\Android\jdk\*\bin\keytool.exe",
        "$env:ProgramFiles\Eclipse Adoptium\jdk-*\bin\keytool.exe",
        "$env:ProgramFiles\Android\Android Studio\jbr\bin\keytool.exe",
        "$env:LOCALAPPDATA\Programs\Android Studio\jbr\bin\keytool.exe"
    )
    foreach ($r in $roots) {
        $hit = Get-Item -Path $r -ErrorAction SilentlyContinue |
               Sort-Object FullName -Descending |
               Select-Object -First 1
        if ($hit) { return $hit.FullName }
    }
    throw "No se encontro keytool.exe. Instale un JDK o defina JAVA_HOME."
}

# --- Validaciones previas ---------------------------------------------------

$keytool = Resolve-Keytool
Write-Host "keytool: $keytool" -ForegroundColor DarkGray

$dir = Split-Path -Parent $KeystorePath
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
$KeystorePath = Join-Path (Resolve-Path $dir).Path (Split-Path -Leaf $KeystorePath)

if (Test-Path $KeystorePath) {
    Write-Host ""
    Write-Warning "Ya existe un keystore en: $KeystorePath"
    Write-Warning "No se sobrescribe. Si genera uno nuevo perdera la capacidad de actualizar"
    Write-Warning "las instalaciones firmadas con el actual. Muevalo o renombrelo a proposito."
    exit 1
}

if (-not $Password) {
    $Password = Read-Host -Prompt 'Contrasena para el keystore (min 6 caracteres)' -AsSecureString
    $confirm  = Read-Host -Prompt 'Confirme la contrasena' -AsSecureString
    $a = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))
    $b = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($confirm))
    if ($a -ne $b) { throw "Las contrasenas no coinciden." }
}

$plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
             [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))
if ($plain.Length -lt 6) { throw "La contrasena debe tener al menos 6 caracteres." }

# --- Generacion -------------------------------------------------------------

$dname = "CN=$CommonName, OU=$OrgUnit, O=$Org, L=$Locality, ST=$State, C=$Country"

Write-Host ""
Write-Host "Generando keystore..." -ForegroundColor Cyan
Write-Host "  Archivo  : $KeystorePath"
Write-Host "  Alias    : $Alias"
Write-Host "  Validez  : $ValidityDays dias"
Write-Host "  DN       : $dname"
Write-Host ""

& $keytool -genkeypair `
    -v `
    -keystore  $KeystorePath `
    -storetype PKCS12 `
    -alias     $Alias `
    -keyalg    RSA `
    -keysize   2048 `
    -validity  $ValidityDays `
    -dname     $dname `
    -storepass $plain `
    -keypass   $plain

if ($LASTEXITCODE -ne 0) { throw "keytool fallo con codigo $LASTEXITCODE." }

# --- Verificacion -----------------------------------------------------------

Write-Host ""
Write-Host "Verificando el keystore generado..." -ForegroundColor Cyan
$listado = & $keytool -list -v -keystore $KeystorePath -storepass $plain 2>&1
if ($LASTEXITCODE -ne 0) { throw "No se pudo leer el keystore recien creado." }

$aliasOk = $listado | Select-String -Pattern "Nombre de Alias:\s*$Alias|Alias name:\s*$Alias" -Quiet
if (-not $aliasOk) { throw "El keystore no contiene el alias '$Alias'." }

$listado | Select-String -Pattern 'SHA1:|SHA-1:|SHA256:|SHA-256:|Valido desde|Valid from' |
    ForEach-Object { "  " + $_.ToString().Trim() }

$plain = $null
[GC]::Collect()

Write-Host ""
Write-Host "Keystore creado correctamente." -ForegroundColor Green
Write-Host ""
Write-Host "Siguientes pasos:" -ForegroundColor Yellow
Write-Host "  1. Respalde $KeystorePath en un lugar seguro FUERA del repositorio."
Write-Host "  2. Guarde la contrasena en el gestor de secretos del equipo."
Write-Host "  3. En LaboresNet.csproj ponga <AndroidKeyStore>True</AndroidKeyStore> en"
Write-Host "     las configuraciones Debug|net10.0-android y Release|net10.0-android;"
Write-Host "     hoy esta en False y la firma personalizada se ignora."
Write-Host "  4. Publique con:"
Write-Host "     dotnet publish LaboresNet/LaboresNet.csproj -f net10.0-android -c Release ``"
Write-Host "       -p:AndroidSigningStorePass=<clave> -p:AndroidSigningKeyPass=<clave>"
