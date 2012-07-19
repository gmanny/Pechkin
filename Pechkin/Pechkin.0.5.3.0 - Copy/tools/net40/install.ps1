param($installPath, $toolsPath, $package, $project)

# set dll to copy to build dir
$wkdll = $project.ProjectItems.Item("wkhtmltox0.dll")

$copyToOutput = $wkdll.Properties.Item("CopyToOutputDirectory")
$copyToOutput.Value = 1