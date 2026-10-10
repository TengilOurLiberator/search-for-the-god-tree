<#
.SYNOPSIS
  Automated contribution checks for Search for the Godtree.

.DESCRIPTION
  Enforces what can be enforced mechanically: pull request description, allowed
  paths per task type and trust tier, recipes, licences, approved generators,
  budgets, naming and forbidden names. Runs on Windows PowerShell 5.1 and
  PowerShell 7. Keep this file pure ASCII.

  Rules are read from -RulesRoot (the BASE branch in CI), content from -RepoRoot
  (the pull request). A pull request can therefore not relax the rules it is
  judged by.

.EXAMPLE
  # Whole-repository validation (no pull request context)
  powershell -File tools/checks/Run-Checks.ps1 -All

.EXAMPLE
  # Pull request validation
  git diff --name-status origin/main...HEAD > changed.txt
  powershell -File tools/checks/Run-Checks.ps1 -ChangedFilesPath changed.txt -PrBodyPath body.md -Author somelogin
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$RulesRoot,
    [string]$ChangedFilesPath,
    [string]$PrBodyPath,
    [string]$Author = '',
    [string]$AuthorAssociation = '',
    [switch]$All
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path }
if (-not $RulesRoot) { $RulesRoot = $RepoRoot }
$RepoRoot = (Resolve-Path $RepoRoot).Path
$RulesRoot = (Resolve-Path $RulesRoot).Path

$script:Failures = 0
$script:Notices = 0

function Write-Fail([string]$Check, [string]$Message) {
    $script:Failures++
    Write-Output ("FAIL   [{0}] {1}" -f $Check, $Message)
}
function Write-Notice([string]$Check, [string]$Message) {
    $script:Notices++
    Write-Output ("NOTICE [{0}] {1}" -f $Check, $Message)
}
function Write-Pass([string]$Check, [string]$Message) {
    Write-Output ("ok     [{0}] {1}" -f $Check, $Message)
}

# ---------------------------------------------------------------- helpers

function Get-Prop($Object, [string]$Name) {
    if ($null -eq $Object) { return $null }
    $p = $Object.PSObject.Properties[$Name]
    if ($null -eq $p) { return $null }
    return $p.Value
}

function Read-TextFile([string]$Path) {
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

function Read-JsonFile([string]$Path) {
    return (Read-TextFile $Path) | ConvertFrom-Json
}

function Convert-GlobToRegex([string]$Glob) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append('^')
    $i = 0
    while ($i -lt $Glob.Length) {
        $c = $Glob[$i]
        if ($c -eq '*') {
            if (($i + 1) -lt $Glob.Length -and $Glob[$i + 1] -eq '*') {
                $i++
                if (($i + 1) -lt $Glob.Length -and $Glob[$i + 1] -eq '/') {
                    $i++
                    [void]$sb.Append('(.*/)?')
                } else {
                    [void]$sb.Append('.*')
                }
            } else {
                [void]$sb.Append('[^/]*')
            }
        } else {
            [void]$sb.Append([regex]::Escape([string]$c))
        }
        $i++
    }
    [void]$sb.Append('$')
    return $sb.ToString()
}

function Test-Glob([string]$Path, $Globs) {
    foreach ($g in @($Globs)) {
        if ($Path -cmatch (Convert-GlobToRegex $g)) { return $true }
    }
    return $false
}

function Get-Extension([string]$Path) {
    $name = [System.IO.Path]::GetFileName($Path)
    $idx = $name.LastIndexOf('.')
    if ($idx -lt 0) { return '' }
    return $name.Substring($idx + 1).ToLowerInvariant()
}

function Get-PngSize([string]$Path) {
    $fs = [System.IO.File]::OpenRead($Path)
    try {
        $buf = New-Object byte[] 24
        $read = $fs.Read($buf, 0, 24)
    } finally { $fs.Dispose() }
    if ($read -lt 24) { return $null }
    $sig = @(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)
    for ($i = 0; $i -lt 8; $i++) { if ($buf[$i] -ne $sig[$i]) { return $null } }
    $w = ([int]$buf[16] -shl 24) -bor ([int]$buf[17] -shl 16) -bor ([int]$buf[18] -shl 8) -bor [int]$buf[19]
    $h = ([int]$buf[20] -shl 24) -bor ([int]$buf[21] -shl 16) -bor ([int]$buf[22] -shl 8) -bor [int]$buf[23]
    return @{ Width = $w; Height = $h }
}

function ConvertFrom-JsonToDictionary([string]$Json) {
    # Nested dictionaries/arrays, with no size limit, on both PowerShell editions.
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        return ($Json | ConvertFrom-Json -AsHashtable)
    }
    Add-Type -AssemblyName System.Web.Extensions
    $ser = New-Object System.Web.Script.Serialization.JavaScriptSerializer
    $ser.MaxJsonLength = [int]::MaxValue
    $ser.RecursionLimit = 512
    return $ser.DeserializeObject($Json)
}

function Get-GlbTriangleCount([string]$Path) {
    # Works for .glb and .vrm (a VRM file is a GLB container). Returns $null if unreadable.
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 20) { return $null }
    if ([System.BitConverter]::ToUInt32($bytes, 0) -ne 0x46546C67) { return $null }   # 'glTF'
    $chunkLen = [int][System.BitConverter]::ToUInt32($bytes, 12)
    if ([System.BitConverter]::ToUInt32($bytes, 16) -ne 0x4E4F534A) { return $null }  # 'JSON'
    if ((20 + $chunkLen) -gt $bytes.Length) { return $null }
    $json = [System.Text.Encoding]::UTF8.GetString($bytes, 20, $chunkLen)
    $doc = ConvertFrom-JsonToDictionary $json
    if ($null -eq $doc -or -not $doc.ContainsKey('meshes')) { return 0 }
    $accessors = @()
    if ($doc.ContainsKey('accessors')) { $accessors = @($doc['accessors']) }
    $total = 0
    foreach ($mesh in @($doc['meshes'])) {
        foreach ($prim in @($mesh['primitives'])) {
            $mode = 4
            if ($prim.ContainsKey('mode')) { $mode = [int]$prim['mode'] }
            $count = 0
            if ($prim.ContainsKey('indices')) {
                $count = [int]$accessors[[int]$prim['indices']]['count']
            } elseif ($prim.ContainsKey('attributes') -and $prim['attributes'].ContainsKey('POSITION')) {
                $count = [int]$accessors[[int]$prim['attributes']['POSITION']]['count']
            }
            if ($mode -eq 4) { $total += [int][math]::Floor($count / 3) }
            elseif ($mode -eq 5 -or $mode -eq 6) { if ($count -ge 3) { $total += ($count - 2) } }
        }
    }
    return $total
}

function Test-PowerOfTwo([int]$n) {
    return ($n -gt 0) -and (($n -band ($n - 1)) -eq 0)
}

function Get-PrSections([string]$Body) {
    $clean = [regex]::Replace($Body, '(?s)<!--.*?-->', '')
    $sections = @{}
    $current = $null
    foreach ($line in ($clean -split "`r?`n")) {
        $m = [regex]::Match($line, '^\s{0,3}#{2,3}\s+(.+?)\s*$')
        if ($m.Success) {
            $current = $m.Groups[1].Value.Trim().ToLowerInvariant()
            $sections[$current] = ''
        } elseif ($null -ne $current) {
            $sections[$current] = $sections[$current] + $line + "`n"
        }
    }
    $out = @{}
    foreach ($k in $sections.Keys) { $out[$k] = $sections[$k].Trim() }
    return $out
}

function Test-Blank([string]$Text) {
    if ($null -eq $Text) { return $true }
    $t = $Text.Trim().Trim('_', '*', '-', ' ').Trim().ToLowerInvariant()
    return ($t -eq '' -or $t -eq 'no response' -or $t -eq 'n/a' -or $t -eq 'none' -or $t -eq 'tbd' -or $t -eq 'todo')
}

# ---------------------------------------------------------------- load rules

$dataDir = Join-Path $RulesRoot 'data'
$rules = $null; $budgets = $null; $trust = $null; $forbidden = $null; $licences = $null; $generators = $null
foreach ($name in 'budgets', 'path-rules', 'trust', 'forbidden-terms', 'licences', 'approved-generators') {
    $p = Join-Path $dataDir ($name + '.json')
    if (-not (Test-Path -LiteralPath $p)) { Write-Fail 'data' "missing data/$name.json"; continue }
    try {
        $obj = Read-JsonFile $p
        switch ($name) {
            'budgets' { $budgets = $obj }
            'path-rules' { $rules = $obj }
            'trust' { $trust = $obj }
            'forbidden-terms' { $forbidden = $obj }
            'licences' { $licences = $obj }
            'approved-generators' { $generators = $obj }
        }
    } catch {
        Write-Fail 'data' ("data/$name.json does not parse: " + $_.Exception.Message)
    }
}
if ($script:Failures -gt 0) {
    Write-Output ''
    Write-Output "RESULT: FAILED before checks could run ($($script:Failures) problem(s) in data/)."
    exit 1
}
Write-Pass 'data' 'rule files parse'

$assetRoots = @($rules.asset_roots)
$generatorById = @{}
foreach ($g in @($generators.generators)) { $generatorById[[string]$g.id] = $g }

function Test-UnderAssetRoot([string]$Path) {
    foreach ($r in $assetRoots) { if ($Path.StartsWith($r)) { return $true } }
    return $false
}

# ---------------------------------------------------------------- changed files

$changed = @()   # objects: Status, Path
if ($ChangedFilesPath) {
    foreach ($line in [System.IO.File]::ReadAllLines((Resolve-Path $ChangedFilesPath).Path)) {
        if ($line.Trim() -eq '') { continue }
        $parts = $line -split "`t"
        if ($parts.Count -lt 2) { Write-Fail 'input' "cannot read changed-files line: $line"; continue }
        $status = $parts[0].Substring(0, 1).ToUpperInvariant()
        if ($status -eq 'R' -or $status -eq 'C') {
            # A rename touches both paths.
            $changed += New-Object psobject -Property @{ Status = 'D'; Path = ($parts[1] -replace '\\', '/') }
            $changed += New-Object psobject -Property @{ Status = 'A'; Path = ($parts[$parts.Count - 1] -replace '\\', '/') }
        } else {
            $changed += New-Object psobject -Property @{ Status = $status; Path = ($parts[1] -replace '\\', '/') }
        }
    }
}

# ---------------------------------------------------------------- pull request description

$taskType = $null
if ($PrBodyPath) {
    $body = Read-TextFile (Resolve-Path $PrBodyPath).Path
    $sections = Get-PrSections $body
    $required = @('issue', 'task type', 'sponsor', 'what was done', 'how it was verified', 'not done or not confirmed', 'third-party material')
    $bodyOk = $true
    foreach ($r in $required) {
        if (-not $sections.ContainsKey($r) -or (Test-Blank $sections[$r])) {
            Write-Fail 'description' "section '$r' is missing or empty (use the pull request template; write 'Nothing' with a reason rather than leaving it blank)"
            $bodyOk = $false
        }
    }
    if ($sections.ContainsKey('issue') -and $sections['issue'] -notmatch '#\d+') {
        Write-Fail 'description' "section 'issue' must reference the claimed issue as #<number>"
        $bodyOk = $false
    }
    if ($sections.ContainsKey('task type')) {
        $tt = ($sections['task type'] -split '\s+')[0].Trim('`', '*', '_', '.', ',').ToLowerInvariant()
        if ($null -ne (Get-Prop $rules.task_types $tt)) {
            $taskType = $tt
        } else {
            $valid = (@($rules.task_types.PSObject.Properties.Name) -join ', ')
            Write-Fail 'description' "task type '$tt' is not one of: $valid"
            $bodyOk = $false
        }
    }
    if ($bodyOk) { Write-Pass 'description' "complete; task type '$taskType'" }
}

# ---------------------------------------------------------------- paths and trust tier

if ($changed.Count -gt 0) {
    $tier = 0
    $login = $Author.ToLowerInvariant()
    if ($AuthorAssociation -eq 'OWNER') {
        $tier = 2
    } elseif ($login -ne '') {
        $t = Get-Prop $trust.tiers $login
        if ($null -ne $t) { $tier = [int]$t }
    }

    $pathFailures = 0
    $restrictedTouched = @()
    foreach ($c in $changed) {
        if (Test-Glob $c.Path $rules.restricted.paths) {
            $restrictedTouched += $c.Path
            if ($tier -lt 2) {
                Write-Fail 'paths' "$($c.Path) is in a restricted area (lead and owner-approved agents only)"
                $pathFailures++
            }
        }
    }
    if ($restrictedTouched.Count -gt 0 -and $tier -ge 2) {
        Write-Notice 'paths' ("touches restricted paths; needs the owner's approval before merge: " + (($restrictedTouched | Select-Object -First 10) -join ', '))
    }

    if ($null -ne $taskType) {
        $tdef = Get-Prop $rules.task_types $taskType
        if ([int]$tdef.min_tier -gt $tier) {
            Write-Fail 'paths' "task type '$taskType' needs trust tier $($tdef.min_tier); author '$Author' is tier $tier (see docs/trust-ladder.md)"
            $pathFailures++
        }
        foreach ($c in $changed) {
            if (-not (Test-Glob $c.Path $tdef.allowed)) {
                Write-Fail 'paths' "$($c.Path) is outside the paths allowed for task type '$taskType'"
                $pathFailures++
            }
        }
    } elseif ($PrBodyPath) {
        # already reported under 'description'
    } else {
        Write-Notice 'paths' 'no pull request description given; task-type path rules not applied'
    }

    foreach ($c in $changed) {
        if ($c.Status -eq 'D') { continue }
        $ext = Get-Extension $c.Path
        if ($c.Path.StartsWith('recipes/') -and (@('json', 'md', 'png', 'jpg') -notcontains $ext)) {
            Write-Fail 'paths' "$($c.Path): recipes/ may hold only .json, .md, .png and .jpg files"
            $pathFailures++
        }
        if ((Test-UnderAssetRoot $c.Path) -and (@($budgets.files.asset_extensions) -notcontains $ext)) {
            Write-Fail 'paths' "$($c.Path): file type .$ext is not an allowed asset type"
            $pathFailures++
        }
    }
    if ($pathFailures -eq 0) { Write-Pass 'paths' "$($changed.Count) changed path(s) allowed for tier $tier" }
}

# ---------------------------------------------------------------- recipes

# Index every recipe in the repository: output path -> recipe.
$recipeDir = Join-Path $RepoRoot 'recipes'
$recipes = @()
if (Test-Path -LiteralPath $recipeDir) {
    foreach ($d in (Get-ChildItem -LiteralPath $recipeDir -Directory)) {
        $rp = Join-Path $d.FullName 'recipe.json'
        if (Test-Path -LiteralPath $rp) {
            $entry = New-Object psobject -Property @{ Id = $d.Name; Rel = "recipes/$($d.Name)/recipe.json"; Dir = $d.FullName; Data = $null; Error = $null }
            try { $entry.Data = Read-JsonFile $rp } catch { $entry.Error = $_.Exception.Message }
            $recipes += $entry
        }
    }
}
$outputOwner = @{}    # asset path -> list of recipe ids
$outputMeta = @{}     # asset path -> output object
foreach ($r in $recipes) {
    if ($null -eq $r.Data) { continue }
    foreach ($o in @(Get-Prop $r.Data 'outputs')) {
        if ($null -eq $o) { continue }
        $op = [string](Get-Prop $o 'path')
        if ($op -eq '') { continue }
        if (-not $outputOwner.ContainsKey($op)) { $outputOwner[$op] = @() }
        $outputOwner[$op] += $r.Id
        $outputMeta[$op] = New-Object psobject -Property @{ Output = $o; Recipe = $r }
    }
}

$changedPaths = @($changed | Where-Object { $_.Status -ne 'D' } | ForEach-Object { $_.Path })
$recipesToCheck = @()
if ($All) {
    $recipesToCheck = $recipes
} else {
    foreach ($r in $recipes) {
        $prefix = "recipes/$($r.Id)/"
        $touched = @($changedPaths | Where-Object { $_.StartsWith($prefix) })
        if ($touched.Count -gt 0) { $recipesToCheck += $r }
    }
}

$modelExts = @($budgets.files.measured_model_extensions) + @($budgets.files.declared_model_extensions)

foreach ($r in $recipesToCheck) {
    $where = $r.Rel
    $before = $script:Failures
    if ($null -ne $r.Error) { Write-Fail 'recipe' "$where does not parse: $($r.Error)"; continue }
    $d = $r.Data

    if ($r.Id -cnotmatch '^(chr|wpn|enm|env|prp|ui|vfx|anm|sfx|mus|vox|con)_[a-z0-9]+(_[a-z0-9]+)*$') {
        Write-Fail 'recipe' "$where folder name '$($r.Id)' must be <category>_<name> in lowercase (see docs/recipes.md)"
    }
    if ([string](Get-Prop $d 'id') -cne $r.Id) { Write-Fail 'recipe' "$where 'id' must equal its folder name '$($r.Id)'" }
    if (Test-Blank ([string](Get-Prop $d 'title'))) { Write-Fail 'recipe' "$where 'title' is missing" }

    $origin = [string](Get-Prop $d 'origin')
    if (@('generated', 'sourced', 'handmade', 'mixed') -notcontains $origin) {
        Write-Fail 'recipe' "$where 'origin' must be generated, sourced, handmade or mixed"
    }
    $lic = [string](Get-Prop $d 'licence')
    if ($lic -cne [string]$licences.project.assets) {
        Write-Fail 'recipe' "$where 'licence' must be $($licences.project.assets) (the project asset licence); found '$lic'"
    }
    # An empty list is valid (no manual steps), so test that the field exists, not that it has a value.
    if ($null -eq $d.PSObject.Properties['cleanup']) {
        Write-Fail 'recipe' "$where 'cleanup' is missing (list the manual steps, or use an empty list if there were none)"
    }

    $steps = @(Get-Prop $d 'steps')
    if ($steps.Count -eq 1 -and $null -eq $steps[0]) { $steps = @() }
    if (($origin -eq 'generated' -or $origin -eq 'mixed') -and $steps.Count -eq 0) {
        Write-Fail 'recipe' "$where origin '$origin' needs at least one entry in 'steps'"
    }
    $n = 0
    foreach ($s in $steps) {
        $n++
        $gid = [string](Get-Prop $s 'generator')
        if ($gid -eq '') {
            Write-Fail 'recipe' "$where step ${n}: 'generator' is missing"
        } elseif (-not $generatorById.ContainsKey($gid)) {
            Write-Fail 'recipe' "$where step ${n}: generator '$gid' is not in data/approved-generators.json"
        } else {
            $gstatus = [string]$generatorById[$gid].status
            if ($gstatus -ne 'approved') {
                Write-Fail 'recipe' "$where step ${n}: generator '$gid' has status '$gstatus', not 'approved'"
            }
        }
        $tool = [string](Get-Prop $s 'tool')
        if ($tool -ne '' -and (-not $generatorById.ContainsKey($tool) -or [string]$generatorById[$tool].status -ne 'approved')) {
            Write-Fail 'recipe' "$where step ${n}: tool '$tool' is not an approved entry in data/approved-generators.json"
        }
        foreach ($key in 'version', 'prompt') {
            if (Test-Blank ([string](Get-Prop $s $key))) {
                if ($key -eq 'prompt' -and ([string](Get-Prop $s $key)).Trim().ToLowerInvariant() -eq 'n/a') { continue }
                Write-Fail 'recipe' "$where step ${n}: '$key' is missing (for tools that take no prompt, write 'n/a')"
            }
        }
        if ($null -eq (Get-Prop $s 'seed')) { Write-Fail 'recipe' "$where step ${n}: 'seed' is missing (use null only if the tool has no seed, and say so in 'notes')" }
        $vram = Get-Prop $s 'vram_gb'
        if ($null -eq $vram -or -not ($vram -is [ValueType]) -or [double]$vram -lt 0) {
            Write-Fail 'recipe' "$where step ${n}: 'vram_gb' must be a number (video memory the step needs; 0 for CPU-only tools)"
        }
        $wf = [string](Get-Prop $s 'workflow')
        if ($wf -ne '') {
            if ($wf -match '(^|[\\/])\.\.([\\/]|$)' -or [System.IO.Path]::IsPathRooted($wf)) {
                Write-Fail 'recipe' "$where step ${n}: 'workflow' must be a file inside the recipe folder"
            } elseif (-not (Test-Path -LiteralPath (Join-Path $r.Dir $wf))) {
                Write-Fail 'recipe' "$where step ${n}: workflow file '$wf' not found in the recipe folder"
            }
        }
    }

    $sources = @(Get-Prop $d 'sources')
    if ($sources.Count -eq 1 -and $null -eq $sources[0]) { $sources = @() }
    if (($origin -eq 'sourced' -or $origin -eq 'mixed') -and $sources.Count -eq 0) {
        Write-Fail 'recipe' "$where origin '$origin' needs at least one entry in 'sources'"
    }
    $n = 0
    foreach ($s in $sources) {
        $n++
        foreach ($key in 'name', 'url', 'author', 'licence') {
            if (Test-Blank ([string](Get-Prop $s $key))) { Write-Fail 'recipe' "$where source ${n}: '$key' is missing" }
        }
        $sl = [string](Get-Prop $s 'licence')
        if ($sl -ne '' -and (@($licences.accepted_asset_input_licences) -cnotcontains $sl)) {
            Write-Fail 'licence' "$where source ${n}: licence '$sl' is not accepted for material folded into assets (accepted: $(@($licences.accepted_asset_input_licences) -join ', '))"
        }
    }

    $outputs = @(Get-Prop $d 'outputs')
    if ($outputs.Count -eq 1 -and $null -eq $outputs[0]) { $outputs = @() }
    if ($outputs.Count -eq 0) { Write-Fail 'recipe' "$where 'outputs' must list the asset files this recipe produced" }
    $hasModel = $false
    foreach ($o in $outputs) {
        $op = [string](Get-Prop $o 'path')
        if ($op -eq '') { Write-Fail 'recipe' "$where an output has no 'path'"; continue }
        if (-not (Test-UnderAssetRoot $op)) { Write-Fail 'recipe' "$where output '$op' is not under an asset folder" }
        elseif (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $op))) { Write-Fail 'recipe' "$where output '$op' does not exist" }
        if ($modelExts -contains (Get-Extension $op)) { $hasModel = $true }
    }
    if ($hasModel) {
        $bc = [string](Get-Prop $d 'budget_class')
        if ($bc -eq '' -or $null -eq (Get-Prop $budgets.models.classes $bc)) {
            $valid = (@($budgets.models.classes.PSObject.Properties.Name) -join ', ')
            Write-Fail 'recipe' "$where has model outputs, so 'budget_class' must be one of: $valid"
        }
    }
    if ($script:Failures -eq $before) { Write-Pass 'recipe' $where }
}

# ---------------------------------------------------------------- assets: recipe coverage, naming, budgets

$assetsToCheck = @()
if ($All) {
    foreach ($root in $assetRoots) {
        $full = Join-Path $RepoRoot $root
        if (Test-Path -LiteralPath $full) {
            foreach ($f in (Get-ChildItem -LiteralPath $full -Recurse -File)) {
                $assetsToCheck += ($f.FullName.Substring($RepoRoot.Length).TrimStart('\', '/') -replace '\\', '/')
            }
        }
    }
    # Outputs whose recipe changed are covered by -All as well.
} else {
    $assetsToCheck = @($changedPaths | Where-Object { Test-UnderAssetRoot $_ })
    # Re-check outputs of any recipe that changed (a budget class may have been edited).
    foreach ($r in $recipesToCheck) {
        if ($null -eq $r.Data) { continue }
        foreach ($o in @(Get-Prop $r.Data 'outputs')) {
            $op = [string](Get-Prop $o 'path')
            if ($op -ne '' -and ($assetsToCheck -notcontains $op) -and (Test-Path -LiteralPath (Join-Path $RepoRoot $op))) { $assetsToCheck += $op }
        }
    }
}

$maxAny = [long]$budgets.files.max_bytes_any_file
foreach ($c in $changedPaths) {
    $full = Join-Path $RepoRoot $c
    if ((Test-Path -LiteralPath $full) -and ((Get-Item -LiteralPath $full).Length -gt $maxAny)) {
        Write-Fail 'budget' "$c is larger than $([math]::Round($maxAny / 1MB)) MB"
    }
}

foreach ($a in $assetsToCheck) {
    $ext = Get-Extension $a
    if ($ext -eq 'meta') { continue }
    $full = Join-Path $RepoRoot $a
    if (-not (Test-Path -LiteralPath $full)) { continue }
    $before = $script:Failures
    $fileName = [System.IO.Path]::GetFileName($a)

    if ($fileName -cnotmatch [string]$budgets.naming.pattern) {
        Write-Fail 'naming' "$a does not match <category>_<name>[_<part>].<ext> in lowercase (see canon/art-bible.md)"
    }
    $category = ($fileName -split '_')[0]

    $owners = @()
    if ($outputOwner.ContainsKey($a)) { $owners = @($outputOwner[$a]) }
    if ($owners.Count -eq 0) {
        Write-Fail 'recipe' "$a has no recipe: add it to 'outputs' of a recipes/<id>/recipe.json"
    } elseif ($owners.Count -gt 1) {
        Write-Fail 'recipe' "$a is listed by more than one recipe: $($owners -join ', ')"
    }
    $meta = $null; $recipeData = $null
    if ($outputMeta.ContainsKey($a)) { $meta = $outputMeta[$a].Output; $recipeData = $outputMeta[$a].Recipe.Data }
    $class = $null
    if ($null -ne $recipeData) {
        $bc = [string](Get-Prop $recipeData 'budget_class')
        if ($bc -ne '') { $class = Get-Prop $budgets.models.classes $bc }
    }

    $size = (Get-Item -LiteralPath $full).Length
    if ($size -gt $maxAny) { Write-Fail 'budget' "$a is larger than $([math]::Round($maxAny / 1MB)) MB" }

    if ($ext -eq 'png') {
        $dim = Get-PngSize $full
        if ($null -eq $dim) {
            Write-Fail 'budget' "$a is not a readable PNG"
        } else {
            $maxPx = [int]$budgets.textures.max_px_default
            $byCat = Get-Prop $budgets.textures.max_px_by_category $category
            if ($null -ne $byCat) { $maxPx = [int]$byCat }
            elseif ($null -ne $class) { $maxPx = [int]$class.max_texture_px }
            if ($dim.Width -gt $maxPx -or $dim.Height -gt $maxPx) {
                Write-Fail 'budget' "$a is $($dim.Width)x$($dim.Height); the limit here is ${maxPx}px"
            }
            $exempt = @($budgets.textures.power_of_two_exempt_categories) -contains $category
            if ($budgets.textures.require_power_of_two -and -not $exempt -and -not ((Test-PowerOfTwo $dim.Width) -and (Test-PowerOfTwo $dim.Height))) {
                Write-Fail 'budget' "$a is $($dim.Width)x$($dim.Height); texture sides must be powers of two"
            }
        }
    } elseif ($ext -eq 'jpg' -or $ext -eq 'tga') {
        Write-Notice 'budget' "${a}: .$ext dimensions are not measured by the checks; prefer .png for textures"
    }

    if (@($budgets.files.measured_model_extensions) -contains $ext) {
        $tris = $null
        try { $tris = Get-GlbTriangleCount $full } catch { $tris = $null }
        if ($null -eq $tris) {
            Write-Fail 'budget' "$a is not a readable binary glTF container"
        } elseif ($null -ne $class -and $tris -gt [int]$class.max_triangles) {
            Write-Fail 'budget' "$a has $tris triangles; budget class allows $($class.max_triangles)"
        } elseif ($null -ne $class) {
            Write-Pass 'budget' "$a measured at $tris triangles (limit $($class.max_triangles))"
        }
    } elseif (@($budgets.files.declared_model_extensions) -contains $ext) {
        $declared = Get-Prop $meta 'triangles'
        if ($null -eq $declared) {
            Write-Fail 'budget' "${a}: .$ext cannot be measured, so its recipe output entry must declare 'triangles'"
        } elseif ($null -ne $class -and [int]$declared -gt [int]$class.max_triangles) {
            Write-Fail 'budget' "$a declares $declared triangles; budget class allows $($class.max_triangles)"
        } else {
            Write-Notice 'budget' "${a}: triangle count $declared is declared, not measured; reviewer should confirm"
        }
    }

    if (@('sfx', 'mus', 'vox') -contains $category) {
        if ($ext -ne [string]$budgets.audio.shipping_format) {
            Write-Fail 'budget' "${a}: shipped audio must be .$($budgets.audio.shipping_format)"
        }
        $cap = Get-Prop $budgets.audio.max_bytes $category
        if ($null -ne $cap -and $size -gt [long]$cap) {
            Write-Fail 'budget' "$a is $([math]::Round($size / 1KB)) KB; limit for '$category' is $([math]::Round([long]$cap / 1KB)) KB"
        }
    }
    if ($script:Failures -eq $before) { Write-Pass 'asset' $a }
}

# ---------------------------------------------------------------- forbidden names

$textExts = @('md', 'json', 'txt', 'yml', 'yaml', 'cs', 'yarn', 'csv', 'shader', 'hlsl', 'uxml', 'uss')
$scanList = @()
if ($All) {
    foreach ($f in (Get-ChildItem -LiteralPath $RepoRoot -Recurse -File)) {
        $rel = ($f.FullName.Substring($RepoRoot.Length).TrimStart('\', '/') -replace '\\', '/')
        if ($rel.StartsWith('.git/') -or $rel.StartsWith('unity/Library/') -or $rel.StartsWith('unity/Temp/')) { continue }
        $scanList += $rel
    }
} else {
    $scanList = $changedPaths
}
$termPatterns = @()
foreach ($t in @($forbidden.terms)) {
    $termPatterns += New-Object psobject -Property @{ Term = [string]$t; Regex = ('(?i)(?<![A-Za-z0-9])' + [regex]::Escape([string]$t) + '(?![A-Za-z0-9])') }
}
$termHits = 0
foreach ($rel in $scanList) {
    if (Test-Glob $rel $forbidden.exempt_paths) { continue }
    foreach ($tp in $termPatterns) {
        if ($rel -match $tp.Regex) { Write-Fail 'canon' "${rel}: path contains a name from an existing work ('$($tp.Term)')"; $termHits++ }
    }
    if ($textExts -notcontains (Get-Extension $rel)) { continue }
    $full = Join-Path $RepoRoot $rel
    if (-not (Test-Path -LiteralPath $full)) { continue }
    $text = Read-TextFile $full
    foreach ($tp in $termPatterns) {
        if ($text -match $tp.Regex) { Write-Fail 'canon' "${rel}: contains a name from an existing work ('$($tp.Term)'); the world is original"; $termHits++ }
    }
}
if ($scanList.Count -gt 0 -and $termHits -eq 0) { Write-Pass 'canon' "no forbidden names in $($scanList.Count) file(s)" }

# ---------------------------------------------------------------- result

Write-Output ''
if ($script:Failures -gt 0) {
    Write-Output "RESULT: FAILED with $($script:Failures) problem(s), $($script:Notices) notice(s)."
    exit 1
}
Write-Output "RESULT: passed, $($script:Notices) notice(s)."
exit 0
