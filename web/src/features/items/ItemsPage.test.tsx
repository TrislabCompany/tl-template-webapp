import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, test, vi } from "vitest";
import ItemsPage from "./ItemsPage";

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

test("lists the items from /api/items", async () => {
  vi.stubGlobal("fetch", vi.fn().mockResolvedValue(json([{ id: 1, name: "First item", done: false }])));

  render(<ItemsPage />);

  expect(await screen.findByLabelText("Name of First item")).toHaveProperty("value", "First item");
  expect(fetch).toHaveBeenCalledWith("/api/items", expect.anything());
});

test("adds an item through the form", async () => {
  const fetch = vi
    .fn()
    .mockResolvedValueOnce(json([]))
    .mockResolvedValueOnce(json({ id: 2, name: "Second item", done: false }, 201));
  vi.stubGlobal("fetch", fetch);

  render(<ItemsPage />);
  await screen.findByText("No items yet.");
  fireEvent.change(screen.getByLabelText("New item"), { target: { value: "Second item" } });
  fireEvent.click(screen.getByRole("button", { name: "Add" }));

  expect(await screen.findByLabelText("Name of Second item")).toBeTruthy();
  expect(fetch).toHaveBeenLastCalledWith("/api/items", expect.objectContaining({ method: "POST" }));
});

test("shows an error when the API fails", async () => {
  vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 503 })));

  render(<ItemsPage />);

  expect((await screen.findByRole("alert")).textContent).toContain("503");
});
