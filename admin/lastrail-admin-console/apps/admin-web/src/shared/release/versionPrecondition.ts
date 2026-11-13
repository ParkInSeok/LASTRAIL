export function canSaveVersionRevision(
  editingRevision: string | null | undefined,
  currentRevision: string | null | undefined,
  readUnavailable: boolean,
  needsReview: boolean
) {
  return !readUnavailable && !needsReview && editingRevision !== undefined &&
    currentRevision !== undefined && editingRevision === currentRevision;
}
