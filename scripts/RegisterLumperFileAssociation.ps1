if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Please run this script as an administrator."
    exit
}

$progID = "Lumper"
$exePath = Join-Path $PWD "Lumper.UI.exe"

try {
    New-Item -Path "Registry::HKEY_CLASSES_ROOT\$progID" -Force
    Set-ItemProperty -Path "Registry::HKEY_CLASSES_ROOT\$progID" -Name "(Default)" -Value "Lumper"

    New-Item -Path "Registry::HKEY_CLASSES_ROOT\$progID\shell\open\command" -Force
    Set-ItemProperty -Path "Registry::HKEY_CLASSES_ROOT\$progID\shell\open\command" -Name "(Default)" -Value "`"$exePath`" `"%1`""

    New-Item -Path "Registry::HKEY_CLASSES_ROOT\.bsp\OpenWithProgids" -Force
    Set-ItemProperty -Path "Registry::HKEY_CLASSES_ROOT\.bsp\OpenWithProgids" -Name "$progID" -Value ""

    Write-Host "Lumper was added to Open With menu for .bsp files."
} catch {
    Write-Host "Error occurred: $_"
}
