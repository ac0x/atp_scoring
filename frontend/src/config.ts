export const API_BASE = import.meta.env.VITE_API_BASE as string | undefined
export const HUB_URL = import.meta.env.VITE_HUB_URL as string | undefined
export const LIVE_HUB_URL = import.meta.env.VITE_LIVE_HUB_URL as string | undefined

export const SCENES = {
  ADS_MS: 5000,
  FED_MS: 5000,
  NAD_MS: 5000,
  H2H_MS: 6000,
} as const

export const ENABLE_LEGACY_APP = import.meta.env.VITE_ENABLE_LEGACY_APP === '1'