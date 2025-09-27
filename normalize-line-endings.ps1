# This script normalizes line endings to LF for all files in the current directory
# and its subdirectories. It's designed to skip common development directories.

# Get all files recursively, including hidden files.
Get-ChildItem -Path . -Recurse -File -Force | ForEach-Object {
    # Skip files in specified directories. The regex handles both Windows and non-Windows path separators.
    if ($_.FullName -notmatch '[\\/]\.git[\\/]' -and $_.FullName -notmatch '[\\/]\.vs[\\/]' -and $_.FullName -notmatch '[\\/]bin[\\/]' -and $_.FullName -notmatch '[\\/]obj[\\/]') {
        try {
            # Read the file's content as a single string.
            $content = [System.IO.File]::ReadAllText($_.FullName)

            # Proceed only if CRLF line endings are found to avoid unnecessary writes.
            if ($content.Contains("`r`n")) {
                # Replace all occurrences of CRLF with LF.
                $newContent = $content.Replace("`r`n", "`n")

                # Write the modified content back to the file.
                # A UTF-8 encoding without a Byte Order Mark (BOM) is used.
                [System.IO.File]::WriteAllText($_.FullName, $newContent, [System.Text.UTF8Encoding]::new($false))

                # Output a message for the processed file.
                Write-Host "Normalized: $($_.FullName)"
            }
        }
        catch {
            # Report any errors encountered during file processing.
            Write-Warning "Could not process file: $($_.FullName). Error: $($_.Exception.Message)"
        }
    }
}

Write-Host "Normalization script finished."
