<#
.SYNOPSIS
    Generates Windows Group Policy ADMX/ADML templates for Git Credential Manager.

.DESCRIPTION
    Parses docs/configuration.md to extract all GCM configuration settings and
    generates GitCredentialManager.admx and en-US/GitCredentialManager.adml in
    the same directory as this script.

.EXAMPLE
    ./generate-admx.ps1

.EXAMPLE
    ./generate-admx.ps1 -ConfigurationMd ../../../docs/configuration.md
#>

param(
    [string]$ConfigurationMd = (Join-Path $PSScriptRoot '../../../docs/configuration.md'),
    [string]$OutputDir       = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$REGISTRY_KEY = 'SOFTWARE\GitCredentialManager\Configuration'
$GP_NS        = 'http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions'
$XSD_NS       = 'http://www.w3.org/2001/XMLSchema'
$XSI_NS       = 'http://www.w3.org/2001/XMLSchema-instance'
$XMLNS_NS     = 'http://www.w3.org/2000/xmlns/'

$categories = @(
    [ordered]@{ Name = 'GitCredentialManager'; Display = 'Git Credential Manager'; Parent = $null;                  Pattern = $null }
    [ordered]@{ Name = 'GCM_Trace2';           Display = 'Trace2';                 Parent = 'GitCredentialManager'; Pattern = '^trace2\.' }
    [ordered]@{ Name = 'GCM_Tracing';          Display = 'Tracing';                Parent = 'GitCredentialManager'; Pattern = '\.trace' }
    [ordered]@{ Name = 'GCM_Credentials';      Display = 'Credential Storage';     Parent = 'GitCredentialManager'; Pattern = 'Store(Path)?$|\.cacheOptions$' }
    [ordered]@{ Name = 'GCM_Authentication';   Display = 'Authentication';         Parent = 'GitCredentialManager'; Pattern = '\.msauth' }
    [ordered]@{ Name = 'GCM_AzureRepos';       Display = 'Azure Repos';            Parent = 'GitCredentialManager'; Pattern = '\.azrepos' }
    [ordered]@{ Name = 'GCM_GitHub';           Display = 'GitHub';                 Parent = 'GitCredentialManager'; Pattern = '\.github' }
    [ordered]@{ Name = 'GCM_Bitbucket';        Display = 'Bitbucket';              Parent = 'GitCredentialManager'; Pattern = '\.bitbucket' }
    [ordered]@{ Name = 'GCM_GitLab';           Display = 'GitLab';                 Parent = 'GitCredentialManager'; Pattern = '\.gitlab' }
    [ordered]@{ Name = 'GCM_General';          Display = 'General';                Parent = 'GitCredentialManager'; Pattern = $null }
)

function New-XmlWriter {
    param([string]$Path)
    $xs = [System.Xml.XmlWriterSettings]::new()
    $xs.Indent          = $true
    $xs.IndentChars     = '  '
    $xs.Encoding        = [System.Text.UTF8Encoding]::new($false)
    $xs.NewLineHandling = [System.Xml.NewLineHandling]::Replace
    return [System.Xml.XmlWriter]::Create($Path, $xs)
}

function Write-AdmlString {
    param($Writer, [string]$Id, [string]$Value)
    $Writer.WriteStartElement('string')
    $Writer.WriteAttributeString('id', $Id)
    $Writer.WriteString($Value)
    $Writer.WriteEndElement()
}

$content = Get-Content -LiteralPath $ConfigurationMd -Raw
$settings = [System.Collections.Generic.List[hashtable]]::new()

# Build a map of link-reference -> URL from the document footer
$linkDefs = @{}
foreach ($ld in [regex]::Matches($content, '(?m)^\[([^\]]+)\]:\s*(\S+)')) {
    $linkDefs[$ld.Groups[1].Value.ToLower()] = $ld.Groups[2].Value
}

# Match any namespace.setting heading plus the description paragraph that
# immediately follows it (up to the first subheading, horizontal rule, or EOF).
$pattern = '(?ms)^### ((\w+)\.(\S+?))(?:\s+_\([^)]+\)_)* *\n\n(.*?)(?=\n####|\n---|\Z)'
foreach ($m in [regex]::Matches($content, $pattern)) {
    $fullKey   = $m.Groups[1].Value
    $namespace = $m.Groups[2].Value
    $valueName = $m.Groups[3].Value

    $rawDesc = $m.Groups[4].Value
    $plain   = $rawDesc

    # Check if this setting supports multiple comma-separated values
    $multiValue = $rawDesc -match 'multiple values separated by commas'

    # Extract enum options from a markdown table if present and not multi-value
    $enumOptions = $null
    if (-not $multiValue) {
        $optRows = [System.Collections.Generic.List[hashtable]]::new()
        foreach ($row in [regex]::Matches($rawDesc, '(?m)^\s*`([^`]+)`[^|\n]*\|([^|\n]+)')) {
            $val = $row.Groups[1].Value
            # Skip placeholder/template values (e.g. [guid], id://[guid])
            if ($val -match '[\[\{]|//') { continue }
            # Strip markdown from the description column
            $desc = $row.Groups[2].Value.Trim() `
                -replace '`([^`]+)`',              '$1' `
                -replace '\*\*(.+?)\*\*',          '$1' `
                -replace '(?<![_\w])_(.+?)_(?![_\w])', '$1' `
                -replace '\\\[',                   '[' `
                -replace '\\\]',                   ']' `
                -replace '\[([^\]]+)\]\[[^\]]*\]', '$1' `
                -replace '\[([^\]]+)\]\([^\)]*\)', '$1' `
                -replace '<https?://[^>]+>',        ''
            $optRows.Add(@{ Value = $val; Id = ($val -replace '[^A-Za-z0-9]', '_'); Display = "$val - $($desc.Trim())" })
        }
        if ($optRows.Count -ge 2) { $enumOptions = $optRows.ToArray() }
    }

    # Strip fenced code blocks
    $plain = $plain -replace '(?ms)```[^`]*?```', ''

    # Strip table header rows (plain word|word lines with no backticks) before pipe processing
    $plain = $plain -replace '(?m)^[A-Za-z][\w ]*(\|[\w ]+)+\s*$', ''

    # Strip inline code markers, keeping the text
    $plain = $plain -replace '`([^`]+)`', '$1'

    # Resolve reference-style links [text][ref]: include URL/path for all known links
    foreach ($ld in $linkDefs.GetEnumerator()) {
        $escaped = [regex]::Escape($ld.Key)
        $plain = $plain -replace "\[([^\]]+)\]\[$escaped\]", "`$1 ($($ld.Value))"
    }
    $plain = $plain -replace '\[([^\]]+)\]\[[^\]]*\]', '$1'

    # Resolve inline links [text](url): keep HTTP URLs, drop relative ones
    $plain = [regex]::Replace($plain, '\[([^\]]+)\]\(([^\)]*)\)', {
        param($match)
        $text = $match.Groups[1].Value
        $url  = $match.Groups[2].Value
        if ($url -match '^https?://') { return "$text ($url)" }
        return $text
    })

    # Preserve bare autolinks <https://...>
    $plain = $plain -replace '<(https?://[^>]+)>', '$1'

    $plain = $plain `
        -replace '\\\[',                              '[' `
        -replace '\\\]',                              ']' `
        -replace '\*\*(.+?)\*\*',                    '$1' `
        -replace '(?<!\*)\*(.+?)(?<!\*)\*(?!\*)',    '$1' `
        -replace '(?<![_\w])_(.+?)_(?![_\w])',       '$1' `
        -replace '(?m)^> ?',                         '' `
        -replace '(?m)^[\-|: ]+$',                   '' `
        -replace '\|',                               ': '
    $plain = ($plain -split '\r?\n' | ForEach-Object { $_.TrimEnd() }) -join "`n"
    $plain = [regex]::Replace($plain, '\n{3,}', "`n`n").Trim()

    $deprecated  = $m.Value -match '_\(deprecated\)_'
    $explainText = if ($plain) { "$plain`n`nCorresponds to git config key: $fullKey" }
                  else         { "Corresponds to git config key: $fullKey" }
    if ($deprecated) { $explainText = "DEPRECATED. $explainText" }

    $policyName = if ($namespace -eq 'trace2') {
        "GCM_trace2_$($valueName -replace '[^A-Za-z0-9]', '_')"
    } else {
        "GCM_$($valueName -replace '[^A-Za-z0-9]', '_')"
    }

    # Split camelCase on lowercase-to-uppercase boundaries only (not digit-to-uppercase)
    $displayName = [regex]::Replace($valueName, '(?<=[a-z])(?=[A-Z])', ' ')
    $displayName = $displayName.Substring(0,1).ToUpper() + $displayName.Substring(1)
    # Preserve known compound brand names that camelCase splitting would break
    $displayName = $displayName -replace '\bGit Hub\b', 'GitHub' -replace '\bGit Lab\b', 'GitLab'

    $matched  = $categories | Where-Object { $_.Pattern -and $fullKey -match $_.Pattern } | Select-Object -First 1
    $category = if ($matched) { $matched.Name } else { 'GCM_General' }

    $settings.Add(@{
        PolicyName  = $policyName
        ValueName   = $fullKey
        Category    = $category
        DisplayName = $displayName
        Explain     = $explainText
        EnumOptions = $enumOptions
    })
}

Write-Host "Parsed $($settings.Count) settings from $(Split-Path $ConfigurationMd -Leaf)"

$admxPath = Join-Path $OutputDir 'GitCredentialManager.admx'
$xw = New-XmlWriter $admxPath

$xw.WriteStartDocument()
$xw.WriteStartElement('policyDefinitions', $GP_NS)
$xw.WriteAttributeString('xmlns', 'xsd', $XMLNS_NS, $XSD_NS)
$xw.WriteAttributeString('xmlns', 'xsi', $XMLNS_NS, $XSI_NS)
$xw.WriteAttributeString('revision', '1.0')
$xw.WriteAttributeString('schemaVersion', '1.0')

$xw.WriteStartElement('policyNamespaces')
$xw.WriteStartElement('target')
$xw.WriteAttributeString('prefix', 'GCM')
$xw.WriteAttributeString('namespace', 'Git.Policies.GitCredentialManager')
$xw.WriteEndElement()
$xw.WriteStartElement('using')
$xw.WriteAttributeString('prefix', 'windows')
$xw.WriteAttributeString('namespace', 'Microsoft.Policies.Windows')
$xw.WriteEndElement()
$xw.WriteEndElement()

$xw.WriteStartElement('supersededAdm')
$xw.WriteAttributeString('fileName', '')
$xw.WriteEndElement()
$xw.WriteStartElement('resources')
$xw.WriteAttributeString('minRequiredRevision', '1.0')
$xw.WriteEndElement()

$xw.WriteStartElement('supportedOn')
$xw.WriteStartElement('definitions')
$xw.WriteStartElement('definition')
$xw.WriteAttributeString('name', 'SUPPORTED_GCM')
$xw.WriteAttributeString('displayName', '$(string.SUPPORTED_GCM)')
$xw.WriteEndElement()
$xw.WriteEndElement()
$xw.WriteEndElement()

$xw.WriteStartElement('categories')
foreach ($cat in $categories) {
    $xw.WriteStartElement('category')
    $xw.WriteAttributeString('name', $cat.Name)
    $xw.WriteAttributeString('displayName', "`$(string.Cat_$($cat.Name))")
    if ($cat.Parent) {
        $xw.WriteStartElement('parentCategory')
        $xw.WriteAttributeString('ref', $cat.Parent)
        $xw.WriteEndElement()
    }
    $xw.WriteEndElement()
}
$xw.WriteEndElement()

$xw.WriteStartElement('policies')
foreach ($s in $settings) {
    $xw.WriteStartElement('policy')
    $xw.WriteAttributeString('name', $s.PolicyName)
    $xw.WriteAttributeString('class', 'Machine')
    $xw.WriteAttributeString('displayName', "`$(string.$($s.PolicyName))")
    $xw.WriteAttributeString('explainText', "`$(string.$($s.PolicyName)_Explain)")
    $xw.WriteAttributeString('presentation', "`$(presentation.$($s.PolicyName))")
    $xw.WriteAttributeString('key', $REGISTRY_KEY)
    $xw.WriteAttributeString('valueName', $s.ValueName)

    $xw.WriteStartElement('parentCategory')
    $xw.WriteAttributeString('ref', $s.Category)
    $xw.WriteEndElement()

    $xw.WriteStartElement('supportedOn')
    $xw.WriteAttributeString('ref', 'SUPPORTED_GCM')
    $xw.WriteEndElement()

    $xw.WriteStartElement('elements')
    if ($s.EnumOptions) {
        $xw.WriteStartElement('enum')
        $xw.WriteAttributeString('id', "$($s.PolicyName)_Enum")
        $xw.WriteAttributeString('valueName', $s.ValueName)
        foreach ($opt in $s.EnumOptions) {
            $xw.WriteStartElement('item')
            $xw.WriteAttributeString('displayName', "`$(string.$($s.PolicyName)_Enum_$($opt.Id))")
            $xw.WriteStartElement('value')
            $xw.WriteStartElement('string')
            $xw.WriteString($opt.Value)
            $xw.WriteEndElement()
            $xw.WriteEndElement()
            $xw.WriteEndElement()
        }
        $xw.WriteEndElement()
    } else {
        $xw.WriteStartElement('text')
        $xw.WriteAttributeString('id', "$($s.PolicyName)_Text")
        $xw.WriteAttributeString('valueName', $s.ValueName)
        $xw.WriteEndElement()
    }
    $xw.WriteEndElement()

    $xw.WriteEndElement()
}
$xw.WriteEndElement()

$xw.WriteEndElement()
$xw.WriteEndDocument()
$xw.Flush()
$xw.Close()

Write-Host "Written: $admxPath"

$enUsDir = Join-Path $OutputDir 'en-US'
if (-not (Test-Path $enUsDir)) { New-Item -ItemType Directory -Path $enUsDir | Out-Null }
$admlPath = Join-Path $enUsDir 'GitCredentialManager.adml'

$xw = New-XmlWriter $admlPath

$xw.WriteStartDocument()
$xw.WriteStartElement('policyDefinitionResources', $GP_NS)
$xw.WriteAttributeString('xmlns', 'xsd', $XMLNS_NS, $XSD_NS)
$xw.WriteAttributeString('xmlns', 'xsi', $XMLNS_NS, $XSI_NS)
$xw.WriteAttributeString('revision', '1.0')
$xw.WriteAttributeString('schemaVersion', '1.0')

$xw.WriteElementString('displayName', 'Git Credential Manager Policy Settings')
$xw.WriteElementString('description', 'Group Policy settings for Git Credential Manager.')

$xw.WriteStartElement('resources')
$xw.WriteStartElement('stringTable')

Write-AdmlString $xw 'SUPPORTED_GCM' 'Git Credential Manager (any version)'
foreach ($cat in $categories) {
    Write-AdmlString $xw "Cat_$($cat.Name)" $cat.Display
}
foreach ($s in $settings) {
    Write-AdmlString $xw $s.PolicyName $s.DisplayName
    Write-AdmlString $xw "$($s.PolicyName)_Explain" $s.Explain
    if ($s.EnumOptions) {
        foreach ($opt in $s.EnumOptions) {
            Write-AdmlString $xw "$($s.PolicyName)_Enum_$($opt.Id)" $opt.Display
        }
    }
}

$xw.WriteEndElement() # stringTable

$xw.WriteStartElement('presentationTable')
foreach ($s in $settings) {
    $xw.WriteStartElement('presentation')
    $xw.WriteAttributeString('id', $s.PolicyName)
    if ($s.EnumOptions) {
        $xw.WriteStartElement('dropdownList')
        $xw.WriteAttributeString('refId', "$($s.PolicyName)_Enum")
        $xw.WriteAttributeString('noSort', 'true')
        $xw.WriteString("$($s.DisplayName):")
        $xw.WriteEndElement()
    } else {
        $xw.WriteStartElement('textBox')
        $xw.WriteAttributeString('refId', "$($s.PolicyName)_Text")
        $xw.WriteElementString('label', "$($s.DisplayName):")
        $xw.WriteEndElement()
    }
    $xw.WriteEndElement()
}
$xw.WriteEndElement() # presentationTable

$xw.WriteEndElement() # resources
$xw.WriteEndElement() # policyDefinitionResources
$xw.WriteEndDocument()
$xw.Flush()
$xw.Close()

Write-Host "Written: $admlPath"
Write-Host "Done. Generated $($settings.Count) policy settings."
