// Calls the items endpoints of the API at the relative path /api. nginx in
// the container, and the Vite dev server locally, pass /api on to the API, so
// the page and the API share one origin and need no CORS settings.

export interface Item {
  id: number;
  name: string;
  done: boolean;
}

async function send<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/items${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
  });
  if (!response.ok) {
    throw new Error(`${init?.method ?? "GET"} /api/items${path}: ${response.status}`);
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export const listItems = () => send<Item[]>("");

export const createItem = (name: string) =>
  send<Item>("", { method: "POST", body: JSON.stringify({ name }) });

export const updateItem = (item: Item) =>
  send<Item>(`/${item.id}`, {
    method: "PUT",
    body: JSON.stringify({ name: item.name, done: item.done }),
  });

export const deleteItem = (id: number) => send<void>(`/${id}`, { method: "DELETE" });
