export function isFresher(nextIso: string, currentIso?: string): boolean {
  if (!currentIso) return true
  const next = Date.parse(nextIso)
  const current = Date.parse(currentIso)
  if (Number.isNaN(next)) return false
  if (Number.isNaN(current)) return true
  return next >= current
}