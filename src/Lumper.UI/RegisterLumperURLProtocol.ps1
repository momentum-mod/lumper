if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Please run this script as an administrator."
    exit
}

$protocolName = "lumper"
$exePath = Join-Path $PWD "Lumper.UI.exe"
$registryPath = "Registry::HKEY_CLASSES_ROOT\$protocolName"

try {
    New-Item -Path $registryPath -Force
    Set-ItemProperty -Path $registryPath -Name "URL Protocol" -Value ""
    New-Item -Path "$registryPath\shell\open\command" -Force
    Set-ItemProperty -Path "$registryPath\shell\open\command" -Name "(Default)" -Value "`"$exePath`" `"%1`""

    Write-Host "Custom URL protocol '$protocolName' registered successfully."
} catch {
    Write-Host "Error occurred: $_"
}

