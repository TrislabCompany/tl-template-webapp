// The one place the app reads environment-specific values.
//
// The browser can't read environment variables, so the container writes
// /config.json when it starts, from every PLATFORM_* setting and every setting
// whose name starts with PUBLIC_ (docker-entrypoint.d/40-config.sh). It holds
// no secrets, and not the API's address: the app calls the API at /api. With
// `pnpm dev` the file doesn't exist, and every value is unset.

export interface AppConfig {
  PLATFORM_ENVIRONMENT?: string;
  PLATFORM_STAMP?: string;
  [name: `PLATFORM_${string}` | `PUBLIC_${string}`]: string | undefined;
}

export async function loadConfig(): Promise<AppConfig> {
  try {
    const response = await fetch("/config.json", { cache: "no-store" });
    return response.ok ? ((await response.json()) as AppConfig) : {};
  } catch {
    return {};
  }
}
