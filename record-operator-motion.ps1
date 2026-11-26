$artifactRoot='D:/Project/LASTRAIL/QA/DirectionImpact-20260921/operator-cinematics-v01-review'
$entry=[IO.File]::ReadAllText("$artifactRoot/handoff-entry.txt")
$marker='operator-cinematics-v01-produced-20261003'
$targets=@('D:/Project/LASTRAIL/docs/handoffs/DIRECTION_2-D-20260913.md','D:/Project/LASTRAIL/docs/handoffs/INTEGRATION_WAITLIST.md','D:/Project/LASTRAIL/docs/handoffs/WORK_QUEUE_DIRECTION.md','D:/Project/LASTRAIL/QA/DirectionImpact-20260921/REVIEW.md')
$utf8=[Text.UTF8Encoding]::new($false)
foreach($path in $targets){
  $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
  try{
    $bytes=[byte[]]::new([int]$stream.Length)
    [void]$stream.Read($bytes,0,$bytes.Length)
    if(-not $utf8.GetString($bytes).Contains($marker)){
      [void]$stream.Seek(0,[IO.SeekOrigin]::End)
      $appendBytes=$utf8.GetBytes($entry)
      $stream.Write($appendBytes,0,$appendBytes.Length)
      $stream.Flush($true)
    }
  }finally{$stream.Dispose()}
  if(-not [IO.File]::ReadAllText($path).Contains($marker)){throw "Record verification failed: $path"}
}
Write-Output 'Direction/WAIT/Queue/QA records verified: 4'
