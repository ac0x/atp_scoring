export const SCENE = {
  LIVE: 'LIVE',
  ADS: 'ADS',
  FINISHED: 'FINISHED',
  UPCOMING: 'UPCOMING',
  ANN_FED: 'ANNOUNCE_FED',
  ANN_NAD: 'ANNOUNCE_NAD',
  ANN_H2H: 'ANNOUNCE_H2H',
  ANN_SIM: 'ANNOUNCE_SIM',
} as const

export type SceneValue = (typeof SCENE)[keyof typeof SCENE]

const canonicalMap = new Map(
  Object.values(SCENE).map((value) => [value.toLowerCase(), value])
)

export function canonicalSceneName(scene: string): SceneValue | string {
  if (!scene) return scene
  const candidate = canonicalMap.get(scene.toLowerCase())
  if (candidate) return candidate
  return scene.toUpperCase()
}