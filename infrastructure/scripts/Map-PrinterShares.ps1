# Auto-map network / cloud printers for domain users
# Run as the logged-on user (not SYSTEM)

param(
    [string]$PrintServer = "printserver.contoso.com",
    [string]$LogPath = "$env:TEMP\PrinterMapping.log"
)

$Printers = @(
    @{ Share = "Reception"; Group = ""; Default = $true },
    @{ Share = "Accounting"; Group = "GG-Accounting"; Default = $false },
    @{ Share = "Warehouse"; Group = "GG-Warehouse"; Default = $false },
    @{ Share = "HR-Secure"; Group = "GG-HR"; Default = $false }
)

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp - $Message" | Out-File -FilePath $LogPath -Append -Encoding UTF8
    Write-Host $Message
}

function Test-ADGroupMembership {
    param([string]$GroupName)

    if ([string]::IsNullOrWhiteSpace($GroupName)) {
        return $true
    }

    try {
        $groups = whoami /groups /fo csv | ConvertFrom-Csv
        return $groups.'Group Name' -contains $GroupName -or $groups.'Group Name' -like "*\$GroupName"
    }
    catch {
        Write-Log "WARNING: Could not check group membership for $GroupName"
        return $false
    }
}

Write-Log "Starting printer mapping for $env:USERNAME"

foreach ($printer in $Printers) {
    $connectionName = "\\$PrintServer\$($printer.Share)"

    if (-not (Test-ADGroupMembership -GroupName $printer.Group)) {
        Write-Log "Skipping $($printer.Share) - user not in group $($printer.Group)"
        continue
    }

    $existing = Get-Printer -Name $connectionName -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Log "Already mapped: $connectionName"
    }
    else {
        try {
            Add-Printer -ConnectionName $connectionName -ErrorAction Stop
            Write-Log "Successfully mapped: $connectionName"
        }
        catch {
            Write-Log "ERROR mapping $connectionName : $($_.Exception.Message)"
            try {
                (New-Object -ComObject WScript.Network).AddWindowsPrinterConnection($connectionName)
                Write-Log "Mapped via COM fallback: $connectionName"
            }
            catch {
                Write-Log "COM fallback also failed for $connectionName"
            }
        }
    }

    if ($printer.Default) {
        try {
            $printerInfo = Get-Printer -Name $connectionName -ErrorAction SilentlyContinue
            if ($printerInfo) {
                (Get-WmiObject -Class Win32_Printer -Filter "Name='$($connectionName.Replace('\','\\'))'").SetDefaultPrinter()
                Write-Log "Set as default: $connectionName"
            }
        }
        catch {
            Write-Log "Could not set default printer: $($_.Exception.Message)"
        }
    }
}

Write-Log "Printer mapping completed"
